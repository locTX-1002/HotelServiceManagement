using BusinessObjects.Entities;

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

    public async Task AddAsync(AuditLog log)
    {
        await using var db = HotelDbContextFactory.Create();
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
