using System.Data;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

/// <summary>DAO Singleton cho check-in/check-out, cap nhat trang thai trong transaction.</summary>
public sealed class StayDAO
{
    private static readonly Lazy<StayDAO> LazyInstance = new(() => new StayDAO());
    private StayDAO() { }
    public static StayDAO Instance => LazyInstance.Value;

    public async Task<List<Stay>> GetActiveAsync()
    {
        await using var context = HotelDbContextFactory.Create();
        return await Query(context).Where(s => s.Status == StayStatus.Active)
            .OrderBy(s => s.Reservation.Room.RoomNumber).ToListAsync();
    }

    public async Task<Stay?> GetByIdAsync(int id)
    {
        await using var context = HotelDbContextFactory.Create();
        return await Query(context).FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Stay?> CheckInAsync(int reservationId, int? userId, DateTime actualCheckIn)
    {
        await using var context = HotelDbContextFactory.Create();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var reservation = await context.Reservations.Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId);
        if (reservation == null || reservation.Status != ReservationStatus.Confirmed
            || reservation.Room.Status is RoomStatus.Occupied or RoomStatus.Maintenance
            || await context.Stays.AnyAsync(s => s.ReservationId == reservationId))
            return null;

        var stay = new Stay
        {
            ReservationId = reservationId,
            ActualCheckIn = actualCheckIn,
            Status = StayStatus.Active,
            CheckedInByUserId = userId,
        };
        reservation.Status = ReservationStatus.CheckedIn;
        reservation.Room.Status = RoomStatus.Occupied;
        context.Stays.Add(stay);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return stay;
    }

    public async Task<Stay?> CheckOutAsync(int stayId, int? userId, DateTime actualCheckOut)
    {
        await using var context = HotelDbContextFactory.Create();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var stay = await context.Stays.Include(s => s.Reservation).ThenInclude(r => r.Room)
            .Include(s => s.Invoice)
            .Include(s => s.ServiceOrders)
            .FirstOrDefaultAsync(s => s.Id == stayId);
        if (stay == null || stay.Status != StayStatus.Active || actualCheckOut < stay.ActualCheckIn)
            return null;
        if (stay.Invoice?.Status != InvoiceStatus.Paid
            || stay.ServiceOrders.Any(o => o.Status is ServiceOrderStatus.Pending or ServiceOrderStatus.Processing))
            return null;

        stay.ActualCheckOut = actualCheckOut;
        stay.CheckedOutByUserId = userId;
        stay.Status = StayStatus.Completed;
        stay.Reservation.Status = ReservationStatus.Completed;
        stay.Reservation.Room.Status = RoomStatus.Cleaning;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return stay;
    }

    private static IQueryable<Stay> Query(HotelDbContext context) => context.Stays.AsNoTracking()
        .Include(s => s.Reservation).ThenInclude(r => r.Guest)
        .Include(s => s.Reservation).ThenInclude(r => r.Room).ThenInclude(r => r.RoomType)
        .Include(s => s.Invoice)
        .Include(s => s.ServiceOrders).ThenInclude(o => o.OrderDetails)
        .Include(s => s.Surcharges);

    /// <summary>Dat phong da xac nhan ma khach chua den quay - danh sach cho check-in.</summary>
    public async Task<List<Reservation>> GetArrivalsAsync()
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Reservations.AsNoTracking()
            .Include(r => r.Guest)
            .Include(r => r.Room).ThenInclude(room => room.RoomType)
            .Where(r => r.Status == ReservationStatus.Confirmed && r.Stay == null)
            .OrderBy(r => r.CheckInDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gia han luu tru: doi ngay tra tren don khi khach dang o muon o them.
    /// Khong co ham nay thi le tan phai check-out roi tao don moi, hong lich su va sai doanh thu.
    /// </summary>
    public async Task<(bool Ok, string Message)> ExtendAsync(int stayId, DateTime newCheckOut)
    {
        await using var context = HotelDbContextFactory.Create();
        var stay = await context.Stays
            .Include(s => s.Reservation).ThenInclude(r => r.Room)
            .FirstOrDefaultAsync(s => s.Id == stayId);

        if (stay == null) return (false, "Khong tim thay luot luu tru.");
        if (stay.Status != StayStatus.Active) return (false, "Chi khach dang luu tru moi gia han duoc.");

        var reservation = stay.Reservation;
        var target = newCheckOut.Date;

        if (target <= stay.ActualCheckIn.Date) return (false, "Ngay tra moi phai sau ngay khach nhan phong.");
        if (target == reservation.CheckOutDate.Date) return (false, "Ngay tra moi trung ngay tra hien tai.");
        if (target < DateTime.Today) return (false, "Ngay tra moi khong duoc o qua khu.");

        if (target > reservation.CheckOutDate.Date)
        {
            var busy = await context.Reservations.AsNoTracking().AnyAsync(other =>
                other.Id != reservation.Id
                && other.RoomId == reservation.RoomId
                && (other.Status == ReservationStatus.Pending
                    || other.Status == ReservationStatus.Confirmed
                    || other.Status == ReservationStatus.CheckedIn)
                && other.CheckInDate < target
                && other.CheckOutDate > reservation.CheckOutDate);
            if (busy) return (false, "Phong da co khach khac dat trong khoang muon o them.");
        }

        var oldDate = reservation.CheckOutDate;
        reservation.CheckOutDate = target;
        await context.SaveChangesAsync();
        var word = target > oldDate.Date ? "gia han" : "rut ngan";
        return (true, $"Da {word} phong {reservation.Room.RoomNumber} toi {target:dd/MM/yyyy}.");
    }
}
