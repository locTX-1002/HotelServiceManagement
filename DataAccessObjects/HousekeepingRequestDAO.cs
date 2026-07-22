using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public sealed class HousekeepingRequestDAO
{
    private static readonly Lazy<HousekeepingRequestDAO> LazyInstance = new(() => new HousekeepingRequestDAO());
    private HousekeepingRequestDAO() { }
    public static HousekeepingRequestDAO Instance => LazyInstance.Value;

    public async Task<List<HousekeepingRequest>> GetAllAsync()
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.HousekeepingRequests.AsNoTracking()
            .Include(x => x.Stay).ThenInclude(x => x.Reservation).ThenInclude(x => x.Room)
            .Include(x => x.HandledByUser)
            .OrderBy(x => x.Status).ThenByDescending(x => x.RequestedAt).ToListAsync();
    }

    public async Task<HousekeepingRequest?> GetByIdAsync(int id)
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.HousekeepingRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> IsStayActiveAsync(int stayId)
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Stays.AnyAsync(x => x.Id == stayId && x.Status == StayStatus.Active);
    }

    public async Task SaveAsync(HousekeepingRequest request, bool add)
    {
        await using var context = HotelDbContextFactory.Create();
        request.Stay = null!;
        request.HandledByUser = null;
        if (add) context.HousekeepingRequests.Add(request); else context.HousekeepingRequests.Update(request);
        await context.SaveChangesAsync();
    }
}
