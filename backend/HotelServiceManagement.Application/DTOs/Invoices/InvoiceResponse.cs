using System;

namespace HotelServiceManagement.Application.DTOs.Invoices
{
    public class InvoiceResponse
    {
        public int InvoiceId { get; set; }
        public int StayId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal RoomCharge { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
