using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

/// <summary>DAO Singleton cho nghiep vu dat phong.</summary>
public sealed class ReservationDAO
{
    private static readonly Lazy<ReservationDAO> LazyInstance = new(() => new ReservationDAO());
    private ReservationDAO() { }
    public static ReservationDAO Instance => LazyInstance.Value;

    public async Task<List<Reservation>> GetAllAsync()
    {
        await using var context = HotelDbContextFactory.Create();
        return await Query(context).OrderByDescending(r => r.CheckInDate).ToListAsync();
    }

    /// <summary>
    /// Don dat phong cua DUNG mot khach. Loc ngay tren DB thay vi tai het roi loc o may
    /// khach - khach tu dang nhap chi duoc phep thay du lieu cua chinh minh.
    /// </summary>
    public async Task<List<Reservation>> GetByGuestAsync(int guestId)
    {
        await using var context = HotelDbContextFactory.Create();
        // Include Stay RIENG o day (Query() chung khong lay) vi man "Hoa don cua toi" phai
        // di tu don -> luot o -> hoa don; thieu Stay thi khach khong bao gio thay hoa don nao.
        // Chi hàm nay tai them, cac man khac giu nguyen truy van nhe nhu cu.
        return await Query(context).Include(r => r.Stay)
            .Where(r => r.GuestId == guestId)
            .OrderByDescending(r => r.CheckInDate).ToListAsync();
    }

    public async Task<List<Reservation>> GetRecentByRoomAsync(int roomId, int count = 5)
    {
        await using var context = HotelDbContextFactory.Create();
        return await Query(context).Include(r => r.Stay)
            .Where(r => r.RoomId == roomId)
            .OrderByDescending(r => r.CheckInDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        await using var context = HotelDbContextFactory.Create();
        return await Query(context).FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> BookingCodeExistsAsync(string code)
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Reservations.AnyAsync(r => r.BookingCode == code);
    }

    public async Task<bool> HasOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut,
        int? excludeId = null)
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Reservations.AnyAsync(r => r.RoomId == roomId
            && (excludeId == null || r.Id != excludeId)
            && (r.Status == ReservationStatus.Pending
                || r.Status == ReservationStatus.Confirmed
                || r.Status == ReservationStatus.CheckedIn)
            && checkIn < r.CheckOutDate && checkOut > r.CheckInDate);
    }

    public async Task AddAsync(Reservation entity)
    {
        await using var context = HotelDbContextFactory.Create();
        context.Reservations.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Reservation entity)
    {
        await using var context = HotelDbContextFactory.Create();
        context.Reservations.Update(entity);
        await context.SaveChangesAsync();
    }

    private static IQueryable<Reservation> Query(HotelDbContext context)
        => context.Reservations.AsNoTracking()
            .Include(r => r.Guest)
            .Include(r => r.Stay)
            .Include(r => r.Room).ThenInclude(room => room.RoomType);

    /// <summary>
    /// Phong con trong trong khoang ngay: khong co dat phong chong lich VA khong co
    /// khach dang o keo sang. Man Dat phong, tab Lich phong va thanh tra cuu o Trang chu deu goi.
    /// </summary>
    /// <summary>
    /// Chuyen cac don da qua ngay nhan phong ma khach khong den sang Khong den.
    ///
    /// Khong co buoc nay thi don treo mai o "Cho xac nhan": truy van phong trong van tinh
    /// no la ban nen phong khong ban lai duoc, con lich phong thi ve mot thanh cua ky nghi
    /// da troi qua. Chay mot lan luc app khoi dong, dung mot cau UPDATE.
    /// </summary>
    public async Task<int> SweepNoShowAsync(DateTime today)
    {
        await using var context = HotelDbContextFactory.Create();
        var stale = await context.Reservations
            .Where(r => (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed)
                        && r.CheckInDate < today.Date
                        && !context.Stays.Any(s => s.ReservationId == r.Id))
            .ToListAsync();

        foreach (var reservation in stale)
        {
            reservation.Status = ReservationStatus.NoShow;
        }
        await context.SaveChangesAsync();
        return stale.Count;
    }

    public async Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut)
    {
        await using var context = HotelDbContextFactory.Create();
        // Khach dang o ma chua tra thi phong con bi giu it nhat toi ngay mai
        var occupancyFloor = DateTime.Today.AddDays(1);
        return await context.Rooms.AsNoTracking()
            .Include(room => room.RoomType)
            .Where(room => room.IsActive
                && room.RoomType.IsActive
                && room.Status != RoomStatus.Maintenance)
            .Where(room => !context.Reservations.Any(r => r.RoomId == room.Id
                && (r.Status == ReservationStatus.Pending
                    || r.Status == ReservationStatus.Confirmed
                    || r.Status == ReservationStatus.CheckedIn)
                && checkIn < r.CheckOutDate && checkOut > r.CheckInDate))
            .Where(room => !context.Stays.Any(stay => stay.Reservation.RoomId == room.Id
                && stay.Status == StayStatus.Active
                && checkIn < (stay.Reservation.CheckOutDate > occupancyFloor
                    ? stay.Reservation.CheckOutDate
                    : occupancyFloor)
                && checkOut > stay.ActualCheckIn))
            .OrderBy(room => room.Floor).ThenBy(room => room.RoomNumber)
            .ToListAsync();
    }
}
