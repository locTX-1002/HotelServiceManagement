using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Repositories;

public interface IApprovalRequestRepository
{
    Task<List<ApprovalRequest>> GetPendingAsync();
    Task<ApprovalRequest?> GetByIdAsync(int id);
    Task<bool> HasPendingAsync(ApprovalRequestType type, int targetId);
    Task SaveAsync(ApprovalRequest request, bool add);
    Task AddAuditAsync(AuditLog log);
}
