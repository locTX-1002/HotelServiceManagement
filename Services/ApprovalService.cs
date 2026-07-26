using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;

namespace Services;

public sealed class ApprovalService : IApprovalService
{
    private readonly IApprovalRequestRepository _requests;
    private readonly IReservationService _reservations;
    private readonly IInvoiceService _invoices;
    private readonly IPaymentService _payments;

    public ApprovalService() : this(new ApprovalRequestRepository(), new ReservationService(),
        new InvoiceService(), new PaymentService()) { }

    public ApprovalService(IApprovalRequestRepository requests, IReservationService reservations,
        IInvoiceService invoices, IPaymentService payments)
    {
        _requests = requests;
        _reservations = reservations;
        _invoices = invoices;
        _payments = payments;
    }

    public async Task<ServiceResult<List<ApprovalRequest>>> GetPendingAsync()
    {
        if (!Enum.GetValues<ApprovalRequestType>().Any(HasApprovePermission))
            return ServiceResult<List<ApprovalRequest>>.Failure("Bạn không có quyền xem danh sách phê duyệt.");
        var all = await _requests.GetPendingAsync();
        return ServiceResult<List<ApprovalRequest>>.Success(
            all.Where(x => HasApprovePermission(x.RequestType)).ToList());
    }

    public async Task<ServiceResult<ApprovalRequest>> RequestAsync(
        ApprovalRequestType type, int targetId, string reason, decimal? requestedValue = null)
    {
        var user = AppSession.CurrentUser;
        if (user == null) return ServiceResult<ApprovalRequest>.Failure("Phiên đăng nhập không hợp lệ.");
        if (!HasRequestPermission(type))
            return ServiceResult<ApprovalRequest>.Failure("Bạn không có quyền gửi loại yêu cầu này.");
        if (targetId <= 0) return ServiceResult<ApprovalRequest>.Failure("Đối tượng yêu cầu không hợp lệ.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            return ServiceResult<ApprovalRequest>.Failure("Lý do phải có từ 1 đến 500 ký tự.");
        if (type == ApprovalRequestType.InvoiceDiscount && requestedValue is not > 0)
            return ServiceResult<ApprovalRequest>.Failure("Số tiền giảm phải lớn hơn 0.");
        if (await _requests.HasPendingAsync(type, targetId))
            return ServiceResult<ApprovalRequest>.Failure("Đối tượng này đã có yêu cầu đang chờ duyệt.");

        var request = new ApprovalRequest
        {
            RequestType = type,
            TargetId = targetId,
            RequestedValue = requestedValue,
            Reason = reason.Trim(),
            RequestedByUserId = user.Id,
            RequestedAt = DateTime.Now
        };
        await _requests.SaveAsync(request, true);
        await AuditAsync("approval.request", request, null, request.Status.ToString(), true);
        return ServiceResult<ApprovalRequest>.Success(request, "Đã gửi yêu cầu cho Quản lý phê duyệt.");
    }

    public async Task<ServiceResult<ApprovalRequest>> ReviewAsync(
        int requestId, bool approve, string? reviewNote)
    {
        var reviewer = AppSession.CurrentUser;
        if (reviewer == null) return ServiceResult<ApprovalRequest>.Failure("Phiên đăng nhập không hợp lệ.");
        var request = await _requests.GetByIdAsync(requestId);
        if (request == null) return ServiceResult<ApprovalRequest>.Failure("Không tìm thấy yêu cầu.");
        if (!HasApprovePermission(request.RequestType))
            return ServiceResult<ApprovalRequest>.Failure("Bạn không có quyền phê duyệt yêu cầu này.");
        if (request.Status != ApprovalRequestStatus.Pending)
            return ServiceResult<ApprovalRequest>.Failure("Yêu cầu này đã được xử lý.");
        if (request.RequestedByUserId == reviewer.Id)
            return ServiceResult<ApprovalRequest>.Failure("Người tạo yêu cầu không được tự phê duyệt.");
        if (!approve && string.IsNullOrWhiteSpace(reviewNote))
            return ServiceResult<ApprovalRequest>.Failure("Phải nhập lý do khi từ chối.");

        var oldStatus = request.Status.ToString();
        if (approve)
        {
            var action = await ExecuteApprovedActionAsync(request);
            if (!action.Ok)
            {
                await AuditAsync("approval.execute", request, oldStatus, action.Message, false);
                return ServiceResult<ApprovalRequest>.Failure(action.Message);
            }
            request.Status = ApprovalRequestStatus.Approved;
        }
        else
        {
            request.Status = ApprovalRequestStatus.Rejected;
        }

        request.ReviewedByUserId = reviewer.Id;
        request.ReviewedAt = DateTime.Now;
        request.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
        await _requests.SaveAsync(request, false);
        await AuditAsync(approve ? "approval.approve" : "approval.reject",
            request, oldStatus, request.Status.ToString(), true);
        return ServiceResult<ApprovalRequest>.Success(request,
            approve ? "Đã duyệt và thực hiện yêu cầu." : "Đã từ chối yêu cầu.");
    }

    private async Task<ServiceResult> ExecuteApprovedActionAsync(ApprovalRequest request)
    {
        switch (request.RequestType)
        {
            case ApprovalRequestType.ReservationCancel:
            {
                var result = await _reservations.CancelAsync(request.TargetId);
                return result.Ok ? ServiceResult.Success(result.Message) : ServiceResult.Failure(result.Message);
            }
            case ApprovalRequestType.InvoiceCancel:
                return await _invoices.CancelAsync(request.TargetId);
            case ApprovalRequestType.PaymentVoid:
            {
                var result = await _payments.VoidAsync(request.TargetId);
                return result.Ok ? ServiceResult.Success(result.Message) : ServiceResult.Failure(result.Message);
            }
            case ApprovalRequestType.InvoiceDiscount:
            {
                var result = await _invoices.ApplyApprovedDiscountAsync(
                    request.TargetId, request.RequestedValue ?? 0);
                return result.Ok ? ServiceResult.Success(result.Message) : ServiceResult.Failure(result.Message);
            }
            default:
                return ServiceResult.Failure("Loại yêu cầu này chưa có bộ xử lý.");
        }
    }

    private static bool HasRequestPermission(ApprovalRequestType type) => type switch
    {
        ApprovalRequestType.ReservationCancel => AuthorizationPolicy.HasPermission(PermissionCodes.ReservationCancelRequest),
        ApprovalRequestType.InvoiceDiscount => AuthorizationPolicy.CanRequestManualDiscount,
        ApprovalRequestType.InvoiceCancel => AuthorizationPolicy.CanRequestInvoiceCancel,
        ApprovalRequestType.PaymentVoid => AuthorizationPolicy.CanRequestPaymentVoid,
        ApprovalRequestType.RoomMaintenance => AuthorizationPolicy.HasPermission(PermissionCodes.RoomMaintenanceRequest),
        _ => false
    };

    private static bool HasApprovePermission(ApprovalRequestType type) => type switch
    {
        ApprovalRequestType.ReservationCancel => AuthorizationPolicy.HasPermission(PermissionCodes.ReservationCancelApprove),
        ApprovalRequestType.InvoiceDiscount => AuthorizationPolicy.CanGiveManualDiscount,
        ApprovalRequestType.InvoiceCancel => AuthorizationPolicy.CanApproveInvoiceCancel,
        ApprovalRequestType.PaymentVoid => AuthorizationPolicy.CanApprovePaymentVoid,
        ApprovalRequestType.RoomMaintenance => AuthorizationPolicy.HasPermission(PermissionCodes.RoomMaintenanceApprove),
        _ => false
    };

    private Task AuditAsync(string action, ApprovalRequest request,
        string? oldValues, string? newValues, bool succeeded)
        => _requests.AddAuditAsync(new AuditLog
        {
            UserId = AppSession.CurrentUser?.Id,
            ActionCode = action,
            EntityType = nameof(ApprovalRequest),
            EntityId = request.Id == 0 ? null : request.Id,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedAt = DateTime.Now,
            Succeeded = succeeded
        });
}
