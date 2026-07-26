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
    public string TargetText => $"#{Request.TargetId}";
    public string ValueText => Request.RequestedValue.HasValue
        ? $"{Request.RequestedValue:N0} đ"
        : "—";
    public string RequesterText => Request.RequestedByUser?.FullName ?? $"Người dùng #{Request.RequestedByUserId}";
    public DateTime RequestedAt => Request.RequestedAt;
    public string Reason => Request.Reason;

    public ApprovalRow(ApprovalRequest request) => Request = request;
}
