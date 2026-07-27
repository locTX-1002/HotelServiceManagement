using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;

namespace Repositories;

public sealed class ApprovalRequestRepository : IApprovalRequestRepository
{
    public Task<List<ApprovalRequest>> GetPendingAsync() => ApprovalRequestDAO.Instance.GetPendingAsync();
    public Task<List<ApprovalRequest>> SearchAsync(
        ApprovalRequestStatus status,
        ApprovalRequestType? type,
        string? requesterKeyword,
        DateTime? fromDate,
        DateTime? toDate)
        => ApprovalRequestDAO.Instance.SearchAsync(status, type, requesterKeyword, fromDate, toDate);
    public Task<ApprovalRequest?> GetByIdAsync(int id) => ApprovalRequestDAO.Instance.GetByIdAsync(id);
    public Task<ApprovalRequest?> GetByIdForReviewAsync(int id)
        => ApprovalRequestDAO.Instance.GetByIdForReviewAsync(id);
    public Task<bool> HasPendingAsync(ApprovalRequestType type, int targetId)
        => ApprovalRequestDAO.Instance.HasPendingAsync(type, targetId);
    public Task SaveAsync(ApprovalRequest request, bool add)
        => ApprovalRequestDAO.Instance.SaveAsync(request, add);
    public Task AddAuditAsync(AuditLog log) => ApprovalRequestDAO.Instance.AddAuditAsync(log);
    public Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation)
        => ApprovalRequestDAO.Instance.ExecuteSerializableAsync(operation);
}
