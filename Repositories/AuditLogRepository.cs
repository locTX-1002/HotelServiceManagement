using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    public Task AddAsync(AuditLog log) => AuditLogDAO.Instance.AddAsync(log);
}
