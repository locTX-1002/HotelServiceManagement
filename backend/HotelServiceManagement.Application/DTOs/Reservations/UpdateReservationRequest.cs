using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Application.DTOs.Reservations;

public class UpdateReservationRequest
{
    public int GuestId { get; set; }
    public int RoomId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
}
