using BusinessObjects.Common;

namespace BusinessObjects.Entities
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public int RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;
    }
}
