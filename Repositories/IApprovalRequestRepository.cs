using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Repositories;

public interface IApprovalRequestRepository
{
    Task<List<ApprovalRequest>> GetPendingAsync();
    Task<List<ApprovalRequest>> SearchAsync(
        ApprovalRequestStatus status,
        ApprovalRequestType? type,
        string? requesterKeyword,
        DateTime? fromDate,
        DateTime? toDate);
    Task<Dictionary<(ApprovalRequestType Type, int TargetId), string>>
        GetTargetDisplayNamesAsync(IReadOnlyCollection<ApprovalRequest> requests);
    Task<ApprovalRequest?> GetByIdAsync(int id);
    Task<ApprovalRequest?> GetByIdForReviewAsync(int id);
    Task<bool> HasPendingAsync(ApprovalRequestType type, int targetId);
    Task<List<int>> GetPendingTargetIdsForGuestAsync(ApprovalRequestType type, int guestId);
    Task SaveAsync(ApprovalRequest request, bool add);
    Task AddAuditAsync(AuditLog log);
    Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation);
}
