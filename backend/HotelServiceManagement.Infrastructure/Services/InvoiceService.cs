using HotelServiceManagement.Application.DTOs.Auth;
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
            var invoice = await QueryInvoices()
                .FirstOrDefaultAsync(i => i.Id == id);

            return invoice == null ? null : ToResponse(invoice);
        }

        public async Task<InvoiceResponse?> GetInvoiceByStayIdAsync(int stayId)
        {
            var invoice = await QueryInvoices()
                .FirstOrDefaultAsync(i => i.StayId == stayId);

            return invoice == null ? null : ToResponse(invoice);
        }

        /// <summary>
        /// Check-out already auto-generates the invoice (see StayService.CheckOutAsync), so most calls
        /// here hit an EXISTING invoice - used to (re)apply a promotion code before payment, or as a
        /// manual repair path if a stay somehow completed without one.
        /// </summary>
        public async Task<AuthServiceResult<InvoiceResponse>> CreateInvoiceAsync(
            int stayId,
            int createdByUserId,
            string? promotionCode = null)
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
                return AuthServiceResult<InvoiceResponse>.Failure("Stay not found.", 404);
            }

            if (stay.Status != StayStatus.Completed || stay.ActualCheckOut == null)
            {
                return AuthServiceResult<InvoiceResponse>.Failure(
                    "Stay must be completed with an actual check-out before an invoice can be created.");
            }

            var invoiceDate = stay.ActualCheckOut.Value;
            var roomCharge = CalculateRoomCharge(stay, invoiceDate);
            var serviceCharge = CalculateServiceCharge(stay);
            var subtotal = roomCharge + serviceCharge;

            var invoice = stay.Invoice;
            var isNewInvoice = invoice == null;

            var trimmedCode = promotionCode?.Trim();

            // Hoá đơn đã thu đủ tiền thì không cho đổi khuyến mãi nữa - áp mã lúc này sẽ hạ TotalAmount
            // xuống dưới số tiền đã thu thật, tạo ra khoản dư thừa không ai theo dõi/hoàn lại.
            if (!isNewInvoice && invoice!.Status == InvoiceStatus.Paid && !string.IsNullOrEmpty(trimmedCode))
            {
                return AuthServiceResult<InvoiceResponse>.Failure(
                    "Invoice is already fully paid. Cannot apply a promotion code after payment is complete.", 409);
            }

            var discountAmount = invoice?.DiscountAmount ?? 0;
            var appliedPromotionCode = invoice?.PromotionCode;
            if (!string.IsNullOrEmpty(trimmedCode))
            {
                var normalizedCode = trimmedCode.ToUpperInvariant();
                var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == normalizedCode);
                if (promotion == null)
                {
                    return AuthServiceResult<InvoiceResponse>.Failure("Promotion code not found.");
                }

                if (!promotion.IsActive)
                {
                    return AuthServiceResult<InvoiceResponse>.Failure("Promotion code is inactive.");
                }

                if (invoiceDate.Date < promotion.StartDate.Date || invoiceDate.Date > promotion.EndDate.Date)
                {
                    return AuthServiceResult<InvoiceResponse>.Failure("Promotion code is not valid for this date.");
                }

                var rawDiscount = promotion.Type == PromotionType.Percentage
                    ? subtotal * (promotion.Value / 100m)
                    : promotion.Value;
                discountAmount = Math.Max(0, Math.Min(rawDiscount, subtotal));
                appliedPromotionCode = promotion.Code;
            }

            var totalAmount = subtotal - discountAmount;

            if (isNewInvoice)
            {
                invoice = new Invoice
                {
                    StayId = stay.Id,
                    InvoiceDate = invoiceDate,
                    CreatedByUserId = createdByUserId,
                    Status = InvoiceStatus.Unpaid
                };
                _context.Invoices.Add(invoice);

                // Cọc đã thu lúc đặt phòng -> tạo Payment thật để tái dùng nguyên vẹn logic
                // tính số dư còn lại (đã hardened chống race-condition) trong ResolveInvoiceStatus/PaymentService,
                // không cần sửa gì ở PaymentService.
                if (stay.Reservation.DepositAmount is > 0 and var deposit)
                {
                    invoice.Payments.Add(new Payment
                    {
                        PaymentDate = stay.Reservation.DepositPaidAt ?? invoiceDate,
                        Amount = deposit,
                        PaymentMethod = stay.Reservation.DepositPaymentMethod ?? PaymentMethod.Cash,
                        Status = PaymentStatus.Completed,
                        TransactionId = $"DEP-{stay.Reservation.BookingCode}",
                        ReceivedByUserId = stay.Reservation.CreatedByUserId,
                    });
                }
            }
            else
            {
                invoice!.CreatedByUserId ??= createdByUserId;
            }

            invoice!.RoomCharge = roomCharge;
            invoice.ServiceCharge = serviceCharge;
            invoice.DiscountAmount = discountAmount;
            invoice.PromotionCode = appliedPromotionCode;
            invoice.TotalAmount = totalAmount;
            invoice.Status = ResolveInvoiceStatus(invoice);

            await _context.SaveChangesAsync();

            return AuthServiceResult<InvoiceResponse>.Success(ToResponse(invoice, stay), "Invoice created successfully.");
        }

        private IQueryable<Invoice> QueryInvoices()
        {
            return _context.Invoices
                .AsNoTracking()
                .Include(i => i.Stay)
                    .ThenInclude(s => s.Reservation);
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

        /// <summary>
        /// Used right after CreateAsync/CreateInvoiceAsync where we already have the Stay in hand
        /// (avoids a second round-trip just to read back the deposit amount).
        /// </summary>
        private static InvoiceResponse ToResponse(Invoice invoice, Stay stay)
        {
            var response = ToResponse(invoice);
            response.DepositAmount = stay.Reservation.DepositAmount;
            return response;
        }

        private static InvoiceResponse ToResponse(Invoice invoice)
        {
            var response = new InvoiceResponse
            {
                InvoiceId = invoice.Id,
                StayId = invoice.StayId,
                InvoiceDate = invoice.InvoiceDate,
                RoomCharge = invoice.RoomCharge,
                ServiceCharge = invoice.ServiceCharge,
                DiscountAmount = invoice.DiscountAmount,
                PromotionCode = invoice.PromotionCode,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status.ToString()
            };

            // Stay chỉ chắc chắn có sẵn khi query đi qua QueryInvoices() (GetByIdAsync/GetInvoiceByStayIdAsync).
            if (invoice.Stay != null)
            {
                response.DepositAmount = invoice.Stay.Reservation?.DepositAmount;
            }

            return response;
        }
    }
}
