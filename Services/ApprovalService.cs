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

    public Task<ServiceResult<List<ApprovalListItem>>> GetPendingAsync()
        => SearchAsync(ApprovalRequestStatus.Pending);

    public async Task<ServiceResult<List<ApprovalListItem>>> SearchAsync(
        ApprovalRequestStatus status,
        ApprovalRequestType? type = null,
        string? requesterKeyword = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        if (status is not (ApprovalRequestStatus.Pending
            or ApprovalRequestStatus.Approved
            or ApprovalRequestStatus.Rejected))
            return ServiceResult<List<ApprovalListItem>>.Failure("Trạng thái phê duyệt không hợp lệ.");
        if (!Enum.GetValues<ApprovalRequestType>().Any(HasApprovePermission))
            return ServiceResult<List<ApprovalListItem>>.Failure("Bạn không có quyền xem danh sách phê duyệt.");
        if (type.HasValue && !HasApprovePermission(type.Value))
            return ServiceResult<List<ApprovalListItem>>.Failure("Bạn không có quyền xem loại yêu cầu này.");
        if (!string.IsNullOrWhiteSpace(requesterKeyword) && requesterKeyword.Trim().Length > 100)
            return ServiceResult<List<ApprovalListItem>>.Failure("Từ khóa người gửi tối đa 100 ký tự.");
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            return ServiceResult<List<ApprovalListItem>>.Failure("Từ ngày không được sau đến ngày.");

        var all = await _requests.SearchAsync(
            status, type, requesterKeyword, fromDate, toDate);
        var visible = all.Where(x => HasApprovePermission(x.RequestType)).ToList();
        var targetNames = await _requests.GetTargetDisplayNamesAsync(visible);
        var items = visible.Select(request =>
        {
            var key = (request.RequestType, request.TargetId);
            var displayName = targetNames.TryGetValue(key, out var name)
                ? name
                : "Đối tượng không còn tồn tại";
            return new ApprovalListItem(request, displayName);
        }).ToList();

        return ServiceResult<List<ApprovalListItem>>.Success(items);
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

        // Tao request va ghi audit trong CUNG transaction. Neu insert AuditLog that bai,
        // ApprovalRequest cung rollback, tranh request ton tai ma khong co dau vet truy vet.
        return await _requests.ExecuteSerializableAsync(async () =>
        {
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
        });
    }

    public async Task<ServiceResult<ApprovalRequest>> ReviewAsync(
        int requestId, bool approve, string? reviewNote)
    {
        var reviewer = AppSession.CurrentUser;
        if (reviewer == null) return ServiceResult<ApprovalRequest>.Failure("Phiên đăng nhập không hợp lệ.");
        if (requestId <= 0) return ServiceResult<ApprovalRequest>.Failure("Yêu cầu không hợp lệ.");
        if (!approve && string.IsNullOrWhiteSpace(reviewNote))
            return ServiceResult<ApprovalRequest>.Failure("Phải nhập lý do khi từ chối.");
        if (!string.IsNullOrWhiteSpace(reviewNote) && reviewNote.Trim().Length > 500)
            return ServiceResult<ApprovalRequest>.Failure("Ghi chú duyệt tối đa 500 ký tự.");

        // Toan bo review chay trong cung transaction Serializable. GetByIdForReviewAsync
        // giu UPDLOCK tren request Pending, nen reviewer thu hai phai cho transaction dau
        // tien ket thuc va se doc lai Approved/Rejected thay vi thuc thi nghiep vu lan 2.
        return await _requests.ExecuteSerializableAsync(async () =>
        {
            var request = await _requests.GetByIdForReviewAsync(requestId);
            if (request == null) return ServiceResult<ApprovalRequest>.Failure("Không tìm thấy yêu cầu.");
            if (!HasApprovePermission(request.RequestType))
                return ServiceResult<ApprovalRequest>.Failure("Bạn không có quyền phê duyệt yêu cầu này.");
            if (request.Status != ApprovalRequestStatus.Pending)
                return ServiceResult<ApprovalRequest>.Failure("Yêu cầu này đã được xử lý.");
            if (request.RequestedByUserId == reviewer.Id)
                return ServiceResult<ApprovalRequest>.Failure("Người tạo yêu cầu không được tự phê duyệt.");

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
        });
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
            case ApprovalRequestType.RoomMaintenance:
            {
                var result = await new RoomService().UpdateStatusAsync(
                    request.TargetId, RoomStatus.Maintenance, canManageMaintenance: true);
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
