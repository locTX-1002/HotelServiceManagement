using System.ComponentModel.DataAnnotations;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Application.DTOs.Reservations;

public class CreateReservationRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "GuestId must be greater than 0.")]
    public int GuestId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RoomId must be greater than 0.")]
    public int RoomId { get; set; }

    // Do not set = 1 here. If FE omits this field, value 0 must be rejected.
    [Range(1, int.MaxValue, ErrorMessage = "NumberOfGuests must be at least 1.")]
    public int NumberOfGuests { get; set; }

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
}