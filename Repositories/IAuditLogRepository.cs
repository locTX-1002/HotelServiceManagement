using BusinessObjects.Entities;

namespace Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
}
