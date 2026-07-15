using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Application.DTOs.Reservations;

public class ReservationResponse
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public int GuestId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string GuestPhoneNumber { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public int NumberOfGuests { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public ReservationStatus Status { get; set; }
    public string? SpecialRequests { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? DepositPaymentMethod { get; set; }
    public DateTime? DepositPaidAt { get; set; }
}