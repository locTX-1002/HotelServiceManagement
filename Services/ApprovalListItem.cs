using BusinessObjects.Entities;

namespace Services;

/// <summary>
/// Read model for the approval list. Display-only data stays outside the EF entity.
/// </summary>
public sealed record ApprovalListItem(ApprovalRequest Request, string TargetDisplayName);
