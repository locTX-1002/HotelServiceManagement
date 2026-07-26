using BusinessObjects.Common;

namespace BusinessObjects.Entities;

public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }
    public virtual User? User { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Succeeded { get; set; }
}
