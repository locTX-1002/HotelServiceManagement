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
            .FirstOrDefaultAsync(s => s.Id == stayId);
        if (stay == null || stay.Status != StayStatus.Active || actualCheckOut < stay.ActualCheckIn)
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
}
