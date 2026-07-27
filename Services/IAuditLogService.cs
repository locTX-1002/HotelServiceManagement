using BusinessObjects.Entities;

namespace Services;

public interface IAuditLogService
{
    Task<ServiceResult<List<AuditLog>>> SearchAsync(DateTime from, DateTime to, int? userId, string? actionCode);
    Task<ServiceResult<List<string>>> GetActionCodesAsync();
}
