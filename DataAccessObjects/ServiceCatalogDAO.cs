using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public sealed class ServiceCatalogDAO
{
    private static readonly Lazy<ServiceCatalogDAO> LazyInstance = new(() => new ServiceCatalogDAO());
    private ServiceCatalogDAO() { }
    public static ServiceCatalogDAO Instance => LazyInstance.Value;

    public async Task<List<ServiceCategory>> GetCategoriesAsync()
    { await using var c = HotelDbContextFactory.Create(); return await c.ServiceCategories.AsNoTracking().OrderBy(x => x.CategoryName).ToListAsync(); }
    public async Task<List<ServiceItem>> GetItemsAsync(bool availableOnly = false)
    { await using var c = HotelDbContextFactory.Create(); var q = c.ServiceItems.AsNoTracking().Include(x => x.ServiceCategory).AsQueryable(); if (availableOnly) q = q.Where(x => x.IsAvailable && x.ServiceCategory.IsActive); return await q.OrderBy(x => x.ServiceName).ToListAsync(); }
    public async Task<ServiceCategory?> GetCategoryAsync(int id)
    { await using var c = HotelDbContextFactory.Create(); return await c.ServiceCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id); }
    public async Task<ServiceItem?> GetItemAsync(int id)
    { await using var c = HotelDbContextFactory.Create(); return await c.ServiceItems.AsNoTracking().Include(x => x.ServiceCategory).FirstOrDefaultAsync(x => x.Id == id); }
    public async Task SaveCategoryAsync(ServiceCategory x, bool add)
    { await using var c = HotelDbContextFactory.Create(); if (add) c.Add(x); else c.Update(x); await c.SaveChangesAsync(); }
    public async Task SaveItemAsync(ServiceItem x, bool add)
    { await using var c = HotelDbContextFactory.Create(); x.ServiceCategory = null!; if (add) c.Add(x); else c.Update(x); await c.SaveChangesAsync(); }
}
