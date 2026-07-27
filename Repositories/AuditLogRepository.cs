using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    public Task<List<AuditLog>> SearchAsync(DateTime from, DateTime to, int? userId, string? actionCode, int take)
        => AuditLogDAO.Instance.SearchAsync(from, to, userId, actionCode, take);

    public Task<List<string>> GetActionCodesAsync() => AuditLogDAO.Instance.GetActionCodesAsync();

    public Task<List<User>> GetActorsAsync() => AuditLogDAO.Instance.GetActorsAsync();

    public Task AddAsync(AuditLog log) => AuditLogDAO.Instance.AddAsync(log);
}
