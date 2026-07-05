using HotelServiceManagement.Domain.Common;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Domain.Entities;

public class Room : BaseAuditableEntity
{
    public int RoomId { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomNumber { get; set; } = null!;
    public int Floor { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public bool IsActive { get; set; } = true;

    public RoomType RoomType { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
