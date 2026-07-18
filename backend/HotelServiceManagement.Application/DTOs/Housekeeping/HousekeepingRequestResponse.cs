using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Application.DTOs.Housekeeping
{
    public class HousekeepingRequestResponse
    {
        public int Id { get; set; }
        public int StayId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public string? Note { get; set; }
        public HousekeepingRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? HandledAt { get; set; }
        public string? HandledByUserName { get; set; }
    }
}
