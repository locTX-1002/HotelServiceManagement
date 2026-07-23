using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

/// <summary>DAO Singleton quan ly ho so khach hang.</summary>
public sealed class GuestDAO
{
    private static readonly Lazy<GuestDAO> LazyInstance = new(() => new GuestDAO());
    private GuestDAO() { }
    public static GuestDAO Instance => LazyInstance.Value;

    public async Task<List<Guest>> GetAllAsync()
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Guests.AsNoTracking().OrderBy(g => g.FullName).ToListAsync();
    }

    public async Task<Guest?> GetByIdAsync(int id)
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Guests.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<List<Guest>> SearchAsync(string keyword)
    {
        var term = keyword.Trim();
        await using var context = HotelDbContextFactory.Create();
        return await context.Guests.AsNoTracking()
            .Where(g => g.FullName.Contains(term)
                || g.PhoneNumber.Contains(term)
                || (g.Email != null && g.Email.Contains(term))
                || (g.IdentityNumber != null && g.IdentityNumber.Contains(term)))
            .OrderBy(g => g.FullName).ToListAsync();
    }

    public async Task<bool> IdentityNumberExistsAsync(string value, int? excludeId = null)
    {
        var normalized = value.Trim();
        await using var context = HotelDbContextFactory.Create();
        return await context.Guests.AnyAsync(g => g.IdentityNumber == normalized
            && (excludeId == null || g.Id != excludeId));
    }

    public async Task<bool> HasReservationsAsync(int guestId)
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Reservations.AnyAsync(r => r.GuestId == guestId);
    }

    public async Task AddAsync(Guest entity)
    {
        await using var context = HotelDbContextFactory.Create();
        context.Guests.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Guest entity)
    {
        await using var context = HotelDbContextFactory.Create();
        context.Guests.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guest entity)
    {
        await using var context = HotelDbContextFactory.Create();
        context.Guests.Remove(entity);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Tra dung mot khach theo CCCD hoac so dien thoai chinh xac.
    /// Dung o buoc dau dialog dat phong: khong ra thi hien form tao khach moi.
    /// </summary>
    public async Task<Guest?> FindExactAsync(string idOrPhone)
    {
        var key = (idOrPhone ?? string.Empty).Trim();
        if (key.Length == 0) return null;
        await using var context = HotelDbContextFactory.Create();
        return await context.Guests.AsNoTracking()
            .FirstOrDefaultAsync(g => g.IdentityNumber == key || g.PhoneNumber == key);
    }
}
