using System;

namespace HotelServiceManagement.Application.DTOs.Stays
{
    public class ActiveStayResponse
    {
        public int StayId { get; set; }
        public int ReservationId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime ActualCheckIn { get; set; }
        public DateTime PlannedCheckOut { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
