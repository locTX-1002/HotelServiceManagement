using System;
using System.Threading.Tasks;
using HotelServiceManagement.Application.DTOs.Invoices;
using HotelServiceManagement.Application.Interfaces;

namespace HotelServiceManagement.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        public async Task<InvoiceResponse?> GetInvoiceByStayIdAsync(int stayId)
        {
            // Placeholder/Skeleton response
            return new InvoiceResponse
            {
                InvoiceId = 1,
                StayId = stayId,
                InvoiceDate = DateTime.UtcNow,
                RoomCharge = 200.00m,
                ServiceCharge = 50.00m,
                TotalAmount = 250.00m,
                Status = "Unpaid"
            };
        }

        public async Task<InvoiceResponse?> CreateInvoiceAsync(int stayId)
        {
            // Placeholder/Skeleton response
            return new InvoiceResponse
            {
                InvoiceId = 1,
                StayId = stayId,
                InvoiceDate = DateTime.UtcNow,
                RoomCharge = 200.00m,
                ServiceCharge = 50.00m,
                TotalAmount = 250.00m,
                Status = "Unpaid"
            };
        }
    }
}
