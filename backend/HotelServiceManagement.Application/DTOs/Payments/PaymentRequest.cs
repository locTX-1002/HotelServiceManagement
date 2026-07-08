using System;

namespace HotelServiceManagement.Application.DTOs.Payments
{
    public class PaymentRequest
    {
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // Cash, BankTransfer, Card
    }
}
