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

    public int RequestedByUserId { get; set; }
    public virtual User RequestedByUser { get; set; } = null!;
    public DateTime RequestedAt { get; set; }

    public int? ReviewedByUserId { get; set; }
    public virtual User? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}
