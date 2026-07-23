using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;
namespace DataAccessObjects;

public sealed class GuestAccountDAO
{
    private static readonly Lazy<GuestAccountDAO> LazyInstance = new(() => new GuestAccountDAO());
    private GuestAccountDAO() { }
    public static GuestAccountDAO Instance => LazyInstance.Value;
    public async Task<GuestAccount?> GetByGuestIdAsync(int id) { await using var c = HotelDbContextFactory.Create(); return await c.GuestAccounts.AsNoTracking().Include(x => x.Guest).FirstOrDefaultAsync(x => x.GuestId == id); }
    public async Task<GuestAccount?> GetByPhoneAsync(string phone) { await using var c = HotelDbContextFactory.Create(); return await c.GuestAccounts.AsNoTracking().Include(x => x.Guest).FirstOrDefaultAsync(x => x.Guest.PhoneNumber == phone); }
    public async Task SaveAsync(GuestAccount x, bool add) { await using var c = HotelDbContextFactory.Create(); x.Guest = null!; if (add) c.GuestAccounts.Add(x); else c.GuestAccounts.Update(x); await c.SaveChangesAsync(); }
}
