using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities;

public class User : BaseAuditableEntity
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public Role Role { get; set; } = null!;
}
