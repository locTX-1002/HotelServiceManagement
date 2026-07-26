using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public sealed class ApprovalRequestDAO
{
    private static readonly Lazy<ApprovalRequestDAO> LazyInstance = new(() => new ApprovalRequestDAO());
    public static ApprovalRequestDAO Instance => LazyInstance.Value;
    private ApprovalRequestDAO() { }

    public async Task<List<ApprovalRequest>> GetPendingAsync()
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.ApprovalRequests.AsNoTracking()
            .Include(x => x.RequestedByUser)
            .Where(x => x.Status == ApprovalRequestStatus.Pending)
            .OrderBy(x => x.RequestedAt)
            .ToListAsync();
    }

    public async Task<ApprovalRequest?> GetByIdAsync(int id)
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.ApprovalRequests.AsNoTracking()
            .Include(x => x.RequestedByUser)
            .Include(x => x.ReviewedByUser)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> HasPendingAsync(ApprovalRequestType type, int targetId)
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.ApprovalRequests.AnyAsync(x =>
            x.RequestType == type && x.TargetId == targetId
            && x.Status == ApprovalRequestStatus.Pending);
    }

    public async Task SaveAsync(ApprovalRequest request, bool add)
    {
        await using var db = HotelDbContextFactory.Create();
        request.RequestedByUser = null!;
        request.ReviewedByUser = null;
        if (add) db.ApprovalRequests.Add(request); else db.ApprovalRequests.Update(request);
        await db.SaveChangesAsync();
    }

    public async Task AddAuditAsync(AuditLog log)
    {
        await using var db = HotelDbContextFactory.Create();
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
