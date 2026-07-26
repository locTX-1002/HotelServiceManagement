using System.Collections.Generic;
using BusinessObjects.Common;

namespace BusinessObjects.Entities
{
    public class Role : BaseEntity
    {
        public string RoleName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSystemRole { get; set; } = true;
        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
