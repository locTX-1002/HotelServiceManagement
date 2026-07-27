using BusinessObjects.Entities;

namespace Repositories;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> SearchAsync(DateTime from, DateTime to, int? userId, string? actionCode, int take);
    Task<List<string>> GetActionCodesAsync();
    Task AddAsync(AuditLog log);
}
