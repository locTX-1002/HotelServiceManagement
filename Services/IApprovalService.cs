using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Services;

public interface IApprovalService
{
    Task<ServiceResult<List<ApprovalRequest>>> GetPendingAsync();
    Task<ServiceResult<ApprovalRequest>> RequestAsync(
        ApprovalRequestType type, int targetId, string reason, decimal? requestedValue = null);
    Task<ServiceResult<ApprovalRequest>> ReviewAsync(int requestId, bool approve, string? reviewNote);
}
