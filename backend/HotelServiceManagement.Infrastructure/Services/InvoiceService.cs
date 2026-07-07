using HotelServiceManagement.Application.DTOs.Invoices;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly HotelDbContext _context;

        public InvoiceService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<InvoiceResponse?> GetByIdAsync(int id)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            return invoice == null ? null : ToResponse(invoice);
        }

        public async Task<InvoiceResponse?> GetInvoiceByStayIdAsync(int stayId)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.StayId == stayId);

            return invoice == null ? null : ToResponse(invoice);
        }

        public async Task<InvoiceResponse?> CreateInvoiceAsync(int stayId)
        {
            var stay = await _context.Stays
                .AsSplitQuery()
                .Include(s => s.Reservation)
                    .ThenInclude(r => r.Room)
                        .ThenInclude(r => r.RoomType)
                .Include(s => s.ServiceOrders)
                .Include(s => s.Invoice)
                    .ThenInclude(i => i!.Payments)
                .FirstOrDefaultAsync(s => s.Id == stayId);

            if (stay == null)
            {
                return null;
            }

            var invoiceDate = stay.ActualCheckOut ?? DateTime.UtcNow;
            var roomCharge = CalculateRoomCharge(stay, invoiceDate);
            var serviceCharge = CalculateServiceCharge(stay);
            var totalAmount = roomCharge + serviceCharge;

            var invoice = stay.Invoice;
            if (invoice == null)
            {
                invoice = new Invoice
                {
                    StayId = stay.Id,
                    InvoiceDate = invoiceDate,
                    Status = InvoiceStatus.Unpaid
                };
                _context.Invoices.Add(invoice);
            }

            invoice.RoomCharge = roomCharge;
            invoice.ServiceCharge = serviceCharge;
            invoice.TotalAmount = totalAmount;
            invoice.Status = ResolveInvoiceStatus(invoice);

            await _context.SaveChangesAsync();

            return ToResponse(invoice);
        }

        private static decimal CalculateRoomCharge(Stay stay, DateTime invoiceDate)
        {
            var nights = Math.Max(1, (invoiceDate.Date - stay.ActualCheckIn.Date).Days);
            return nights * stay.Reservation.Room.RoomType.BasePrice;
        }

        private static decimal CalculateServiceCharge(Stay stay)
        {
            return stay.ServiceOrders
                .Where(o => o.Status != ServiceOrderStatus.Cancelled)
                .Sum(o => o.TotalAmount);
        }

        private static InvoiceStatus ResolveInvoiceStatus(Invoice invoice)
        {
            var paidAmount = invoice.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            if (paidAmount <= 0)
            {
                return InvoiceStatus.Unpaid;
            }

            return paidAmount >= invoice.TotalAmount
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;
        }

        private static InvoiceResponse ToResponse(Invoice invoice)
        {
            return new InvoiceResponse
            {
                InvoiceId = invoice.Id,
                StayId = invoice.StayId,
                InvoiceDate = invoice.InvoiceDate,
                RoomCharge = invoice.RoomCharge,
                ServiceCharge = invoice.ServiceCharge,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status.ToString()
            };
        }
    }
}
