using BusinessObjects.Common;
using BusinessObjects.Enums;

namespace BusinessObjects.Entities;

public class ApprovalRequest : BaseEntity
{
    public ApprovalRequestType RequestType { get; set; }
    public int TargetId { get; set; }
    public decimal? RequestedValue { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ApprovalRequestStatus Status { get; set; } = ApprovalRequestStatus.Pending;

    // Request co the do NHAN VIEN hoac KHACH HANG gui. Hai FK deu nullable de giu
    // dung danh tinh nguoi gui thay vi gia mao guest thanh mot User noi bo.
    public int? RequestedByUserId { get; set; }
    public virtual User? RequestedByUser { get; set; }
    public int? RequestedByGuestId { get; set; }
    public virtual Guest? RequestedByGuest { get; set; }
    public DateTime RequestedAt { get; set; }

    public int? ReviewedByUserId { get; set; }
    public virtual User? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}
