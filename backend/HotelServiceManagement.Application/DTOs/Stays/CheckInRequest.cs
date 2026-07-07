using System;

namespace HotelServiceManagement.Application.DTOs.Stays
{
    public class CheckInRequest
    {
        public int ReservationId { get; set; }
        public DateTime ActualCheckIn { get; set; }
    }
}
