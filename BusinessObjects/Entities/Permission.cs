using BusinessObjects.Common;

namespace BusinessObjects.Entities;

public class Permission : BaseEntity
{
    public string PermissionCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
