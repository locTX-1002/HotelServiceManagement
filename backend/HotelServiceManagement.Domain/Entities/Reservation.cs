using HotelServiceManagement.Domain.Common;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Domain.Entities;

public class Reservation : BaseAuditableEntity
{
    public int ReservationId { get; set; }
    public int GuestId { get; set; }
    public int RoomId { get; set; }
    public string BookingCode { get; set; } = null!;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public Guest Guest { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public Stay? Stay { get; set; }
}
