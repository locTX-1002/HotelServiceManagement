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
            .Include(r => r.Room).ThenInclude(room => room.RoomType);

    /// <summary>
    /// Phong con trong trong khoang ngay: khong co dat phong chong lich VA khong co
    /// khach dang o keo sang. Man Dat phong, tab Lich phong va thanh tra cuu o Trang chu deu goi.
    /// </summary>
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
