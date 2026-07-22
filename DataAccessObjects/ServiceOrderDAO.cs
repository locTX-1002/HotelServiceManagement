using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public sealed class ServiceOrderDAO
{
    private static readonly Lazy<ServiceOrderDAO> LazyInstance = new(() => new ServiceOrderDAO());
    private ServiceOrderDAO() { }
    public static ServiceOrderDAO Instance => LazyInstance.Value;

    public async Task<List<ServiceOrder>> GetByStayAsync(int stayId)
    { await using var c = HotelDbContextFactory.Create(); return await c.ServiceOrders.AsNoTracking().Include(x => x.OrderDetails).ThenInclude(x => x.ServiceItem).Where(x => x.StayId == stayId).OrderByDescending(x => x.OrderDate).ToListAsync(); }
    public async Task<bool> IsStayActiveAsync(int stayId)
    { await using var c = HotelDbContextFactory.Create(); return await c.Stays.AnyAsync(x => x.Id == stayId && x.Status == StayStatus.Active); }
    public async Task AddAsync(ServiceOrder order)
    { await using var c = HotelDbContextFactory.Create(); c.ServiceOrders.Add(order); await c.SaveChangesAsync(); }
    public async Task<ServiceOrder?> ChangeStatusAsync(int id, ServiceOrderStatus status)
    { await using var c = HotelDbContextFactory.Create(); var x = await c.ServiceOrders.FirstOrDefaultAsync(o => o.Id == id); if (x == null) return null; x.Status = status; await c.SaveChangesAsync(); return x; }
}
