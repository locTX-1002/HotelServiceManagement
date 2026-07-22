using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public sealed class PromotionDAO
{
    private static readonly Lazy<PromotionDAO> LazyInstance = new(() => new PromotionDAO());
    private PromotionDAO() { }
    public static PromotionDAO Instance => LazyInstance.Value;

    public async Task<List<Promotion>> GetAllAsync()
    { await using var c = HotelDbContextFactory.Create(); return await c.Promotions.AsNoTracking().OrderByDescending(x => x.StartDate).ToListAsync(); }
    public async Task<Promotion?> GetByIdAsync(int id)
    { await using var c = HotelDbContextFactory.Create(); return await c.Promotions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id); }
    public async Task<Promotion?> GetByCodeAsync(string code)
    { await using var c = HotelDbContextFactory.Create(); return await c.Promotions.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code); }
    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    { await using var c = HotelDbContextFactory.Create(); return await c.Promotions.AnyAsync(x => x.Code == code && (excludeId == null || x.Id != excludeId)); }
    public async Task SaveAsync(Promotion x, bool add)
    { await using var c = HotelDbContextFactory.Create(); if (add) c.Promotions.Add(x); else c.Promotions.Update(x); await c.SaveChangesAsync(); }
}
