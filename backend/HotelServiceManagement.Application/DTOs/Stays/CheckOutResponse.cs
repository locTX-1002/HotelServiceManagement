using System;

namespace HotelServiceManagement.Application.DTOs.Stays
{
    public class CheckOutResponse
    {
        public int StayId { get; set; }
        public DateTime ActualCheckOut { get; set; }
        public decimal TotalRoomCharges { get; set; }
        public decimal TotalServiceCharges { get; set; }
        public decimal TotalSurchargeCharges { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
