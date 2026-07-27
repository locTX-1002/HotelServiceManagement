using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Services;

public interface IApprovalService
{
    Task<ServiceResult<List<ApprovalListItem>>> GetPendingAsync();
    Task<ServiceResult<List<ApprovalListItem>>> SearchAsync(
        ApprovalRequestStatus status,
        ApprovalRequestType? type = null,
        string? requesterKeyword = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
    Task<ServiceResult<ApprovalRequest>> RequestAsync(
        ApprovalRequestType type, int targetId, string reason, decimal? requestedValue = null);

    /// <summary>Khach dang nhap tu gui yeu cau huy DON CUA CHINH MINH.</summary>
    Task<ServiceResult<ApprovalRequest>> RequestGuestReservationCancellationAsync(
        int reservationId, string reason);
    Task<ServiceResult<HashSet<int>>> GetMyPendingReservationCancellationIdsAsync();

    Task<ServiceResult<ApprovalRequest>> ReviewAsync(int requestId, bool approve, string? reviewNote);
}
