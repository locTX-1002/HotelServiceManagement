using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace FUHotelManagementWPF.ViewModels.Approvals;

public sealed class ApprovalRow
{
    public ApprovalRequest Request { get; }

    public string TypeText => Request.RequestType switch
    {
        ApprovalRequestType.ReservationCancel => "Huỷ đặt phòng",
        ApprovalRequestType.InvoiceDiscount => "Giảm giá hoá đơn",
        ApprovalRequestType.InvoiceCancel => "Huỷ hoá đơn",
        ApprovalRequestType.PaymentVoid => "Huỷ giao dịch",
        ApprovalRequestType.RoomMaintenance => "Bảo trì phòng",
        _ => "Yêu cầu khác"
    };

    public string StatusText => Request.Status switch
    {
        ApprovalRequestStatus.Pending => "Chờ duyệt",
        ApprovalRequestStatus.Approved => "Đã duyệt",
        ApprovalRequestStatus.Rejected => "Đã từ chối",
        ApprovalRequestStatus.Cancelled => "Đã huỷ",
        _ => Request.Status.ToString()
    };

    // TargetId la khoa noi bo. UI uu tien ten nguoi/phong de nguoi duyet hieu ngay doi tuong.
    public string TargetText => string.IsNullOrWhiteSpace(Request.TargetDisplayName)
        ? "Đối tượng không còn tồn tại"
        : Request.TargetDisplayName;

    public bool IsInvoiceDiscount => Request.RequestType == ApprovalRequestType.InvoiceDiscount;

    public string ValueText => IsInvoiceDiscount && Request.RequestedValue.HasValue
        ? $"{Request.RequestedValue:N0} đ"
        : "—";

    public string RequesterText => Request.RequestedByUser?.FullName
        ?? $"Người dùng #{Request.RequestedByUserId}";

    public string ReviewerText => Request.ReviewedByUser?.FullName
        ?? (Request.ReviewedByUserId.HasValue ? $"Người dùng #{Request.ReviewedByUserId}" : "—");

    public string ReviewedAtText => Request.ReviewedAt.HasValue
        ? Request.ReviewedAt.Value.ToString("dd/MM/yyyy HH:mm")
        : "—";

    public string ReviewNoteText => string.IsNullOrWhiteSpace(Request.ReviewNote)
        ? "—"
        : Request.ReviewNote;

    public string ActionSummaryText => Request.RequestType switch
    {
        ApprovalRequestType.ReservationCancel => $"Huỷ đặt phòng của {TargetText}",
        ApprovalRequestType.InvoiceDiscount => $"Giảm giá hoá đơn của {TargetText}",
        ApprovalRequestType.InvoiceCancel => $"Huỷ hoá đơn của {TargetText}",
        ApprovalRequestType.PaymentVoid => $"Huỷ giao dịch thanh toán của {TargetText}",
        ApprovalRequestType.RoomMaintenance => $"Đưa {TargetText} vào bảo trì",
        _ => $"{TypeText}: {TargetText}"
    };

    public string AffectedDataText => IsInvoiceDiscount && Request.RequestedValue.HasValue
        ? $"{ActionSummaryText}; số tiền đề nghị giảm: {ValueText}."
        : $"{ActionSummaryText}.";

    public string HistoryTitleText => Request.Status switch
    {
        ApprovalRequestStatus.Approved => "Chi tiết yêu cầu đã duyệt",
        ApprovalRequestStatus.Rejected => "Chi tiết yêu cầu đã từ chối",
        _ => "Chi tiết yêu cầu"
    };

    public DateTime RequestedAt => Request.RequestedAt;
    public string RequestedAtText => Request.RequestedAt.ToString("dd/MM/yyyy HH:mm");
    public string Reason => Request.Reason;

    public ApprovalRow(ApprovalRequest request) => Request = request;
}
