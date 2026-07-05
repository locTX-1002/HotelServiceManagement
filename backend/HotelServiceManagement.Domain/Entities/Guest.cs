using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities;

public class Guest : BaseAuditableEntity
{
    public int GuestId { get; set; }
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? IdentityNumber { get; set; }
    public string? Address { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
