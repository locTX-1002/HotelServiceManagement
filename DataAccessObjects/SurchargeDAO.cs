using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;
namespace DataAccessObjects;

public sealed class SurchargeDAO
{
    private static readonly Lazy<SurchargeDAO> LazyInstance = new(() => new SurchargeDAO()); private SurchargeDAO() { }
    public static SurchargeDAO Instance => LazyInstance.Value;
    public async Task<List<SurchargeItem>> GetItemsAsync() { await using var c = HotelDbContextFactory.Create(); return await c.SurchargeItems.AsNoTracking().OrderBy(x => x.Name).ToListAsync(); }
    public async Task<SurchargeItem?> GetItemAsync(int id) { await using var c = HotelDbContextFactory.Create(); return await c.SurchargeItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id); }
    public async Task SaveItemAsync(SurchargeItem x, bool add) { await using var c = HotelDbContextFactory.Create(); if (add) c.Add(x); else c.Update(x); await c.SaveChangesAsync(); }
    public async Task<List<Surcharge>> GetByStayAsync(int id) { await using var c = HotelDbContextFactory.Create(); return await c.Surcharges.AsNoTracking().Include(x => x.SurchargeItem).Where(x => x.StayId == id).ToListAsync(); }
    public async Task<Surcharge?> AddToStayAsync(int stayId, int itemId, int quantity, int? userId) { await using var c = HotelDbContextFactory.Create(); var stay = await c.Stays.FirstOrDefaultAsync(x => x.Id == stayId && x.Status == StayStatus.Active); var item = await c.SurchargeItems.FirstOrDefaultAsync(x => x.Id == itemId && x.IsActive); if (stay == null || item == null) return null; var x = new Surcharge { StayId = stayId, SurchargeItemId = itemId, Quantity = quantity, UnitPriceSnapshot = item.UnitPrice, Subtotal = item.UnitPrice * quantity, CreatedByUserId = userId, CreatedAt = DateTime.Now }; c.Add(x); await c.SaveChangesAsync(); x.SurchargeItem = item; return x; }
    public async Task<Surcharge?> UpdateAsync(int id, int quantity)
    {
        await using var c = HotelDbContextFactory.Create();
        var x = await c.Surcharges.Include(s => s.SurchargeItem).Include(s => s.Stay)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (x == null || x.Stay.Status != StayStatus.Active || await HasCompletedPaymentAsync(c, x.StayId)) return null;
        x.Quantity = quantity;
        x.Subtotal = x.UnitPriceSnapshot * quantity;
        await c.SaveChangesAsync();
        return x;
    }
    public async Task<bool> DeleteAsync(int id)
    {
        await using var c = HotelDbContextFactory.Create();
        var x = await c.Surcharges.Include(s => s.Stay).FirstOrDefaultAsync(s => s.Id == id);
        if (x == null || x.Stay.Status != StayStatus.Active || await HasCompletedPaymentAsync(c, x.StayId)) return false;
        c.Surcharges.Remove(x);
        await c.SaveChangesAsync();
        return true;
    }
    private static Task<bool> HasCompletedPaymentAsync(HotelDbContext c, int stayId) =>
        c.Payments.AnyAsync(p => p.Invoice.StayId == stayId && p.Status == PaymentStatus.Completed);
}
