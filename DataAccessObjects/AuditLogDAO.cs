using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

/// <summary>
/// Doc/ghi nhat ky he thong. Bang AuditLogs da co san tu migration AddApprovalWorkflow,
/// truoc day chi co quy trinh phe duyet ghi vao - khong ai doc ra duoc.
/// </summary>
public sealed class AuditLogDAO
{
    private static readonly Lazy<AuditLogDAO> LazyInstance = new(() => new AuditLogDAO());
    public static AuditLogDAO Instance => LazyInstance.Value;
    private AuditLogDAO() { }

    /// <summary>
    /// Nhat ky chi de tra cuu nen luon lay dong moi nhat truoc va CAT o <paramref name="take"/>
    /// dong: bang nay chi phinh ra theo thoi gian, khong bao gio co ai xoa bot.
    /// </summary>
    public async Task<List<AuditLog>> SearchAsync(DateTime from, DateTime to, int? userId, string? actionCode, int take)
    {
        await using var db = HotelDbContextFactory.Create();
        var query = db.AuditLogs.AsNoTracking().Include(x => x.User).ThenInclude(u => u!.Role)
            .Where(x => x.CreatedAt >= from && x.CreatedAt < to);

        if (userId != null)
        {
            query = query.Where(x => x.UserId == userId);
        }
        if (!string.IsNullOrWhiteSpace(actionCode))
        {
            query = query.Where(x => x.ActionCode == actionCode);
        }

        return await query.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync();
    }

    /// <summary>Danh sach ma hanh dong da tung xuat hien - de do bo loc thay vi go tay.</summary>
    public async Task<List<string>> GetActionCodesAsync()
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.AuditLogs.AsNoTracking()
            .Select(x => x.ActionCode).Distinct().OrderBy(x => x).ToListAsync();
    }

    public async Task AddAsync(AuditLog log)
    {
        await using var db = HotelDbContextFactory.Create();
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
