using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Services;

public interface IApprovalService
{
    Task<ServiceResult<List<ApprovalRequest>>> GetPendingAsync();
    Task<ServiceResult<List<ApprovalRequest>>> SearchAsync(
        ApprovalRequestStatus status,
        ApprovalRequestType? type = null,
        string? requesterKeyword = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
    Task<ServiceResult<ApprovalRequest>> RequestAsync(
        ApprovalRequestType type, int targetId, string reason, decimal? requestedValue = null);
    Task<ServiceResult<ApprovalRequest>> ReviewAsync(int requestId, bool approve, string? reviewNote);
}
