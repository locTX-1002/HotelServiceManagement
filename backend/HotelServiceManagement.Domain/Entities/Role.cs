using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities;

public class Role : BaseAuditableEntity
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
