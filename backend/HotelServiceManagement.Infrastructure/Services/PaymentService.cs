using HotelServiceManagement.Application.DTOs.Payments;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly HotelDbContext _context;

        public PaymentService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, int receivedByUserId)
        {
            if (request == null || request.InvoiceId <= 0)
            {
                return Failure("InvoiceId is required.");
            }

            if (request.Amount <= 0)
            {
                return Failure("Payment amount must be greater than 0.");
            }

            if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out var paymentMethod))
            {
                return Failure("PaymentMethod must be Cash, BankTransfer, or Card.");
            }

            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId);

            if (invoice == null)
            {
                return Failure("Invoice not found.");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                return Failure("Cancelled invoices cannot be paid.");
            }

            var paidAmount = invoice.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);
            var remainingAmount = invoice.TotalAmount - paidAmount;

            if (remainingAmount <= 0 || invoice.Status == InvoiceStatus.Paid)
            {
                return Failure("Invoice has already been fully paid.");
            }

            if (request.Amount > remainingAmount)
            {
                return Failure($"Payment amount exceeds remaining balance. Remaining amount: {remainingAmount:0.00}.");
            }

            var payment = new Payment
            {
                InvoiceId = invoice.Id,
                PaymentDate = DateTime.UtcNow,
                Amount = request.Amount,
                PaymentMethod = paymentMethod,
                Status = PaymentStatus.Completed,
                ReceivedByUserId = receivedByUserId,
                TransactionId = $"PMT-{Guid.NewGuid():N}"
            };

            _context.Payments.Add(payment);

            var newPaidAmount = paidAmount + payment.Amount;
            invoice.Status = newPaidAmount >= invoice.TotalAmount
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;

            await _context.SaveChangesAsync();

            return new PaymentResponse
            {
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod.ToString(),
                Status = payment.Status.ToString(),
                TransactionId = payment.TransactionId,
                IsSuccess = true,
                Message = "Payment recorded successfully."
            };
        }

        private static PaymentResponse Failure(string message)
        {
            return new PaymentResponse
            {
                IsSuccess = false,
                Message = message
            };
        }
    }
}
