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
}
