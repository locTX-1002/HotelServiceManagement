using System.Data;
using HotelServiceManagement.Application.DTOs.Payments;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        // Số dư còn lại + tạo Payment phải là 1 thao tác nguyên tử: nếu chỉ đọc rồi ghi như cũ,
        // 2 request đồng thời (vd. spam-click "Xác nhận thu tiền") cùng đọc thấy còn nợ, cùng
        // qua được kiểm tra, và cả 2 đều tạo Payment thật - khách bị thu tiền nhiều lần dù UI chỉ hiện 1 lần.
        private const int MaxConcurrencyRetries = 5;

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

            for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
            {
                // Serializable: khoá dòng Invoice đang đọc cho tới khi transaction này commit/rollback,
                // request đồng thời khác phải đợi (hoặc bị báo xung đột để thử lại) thay vì đọc được
                // số dư còn lại đã cũ.
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
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
                    await transaction.CommitAsync();

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
                catch (Exception ex) when (IsConcurrencyConflict(ex) && attempt < MaxConcurrencyRetries)
                {
                    // Request khác vừa thanh toán/đổi hoá đơn này cùng lúc - dữ liệu vừa đọc đã cũ,
                    // rollback và đọc lại số dư mới nhất ở vòng lặp kế tiếp thay vì báo lỗi oan.
                    await transaction.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    // Delay ngẫu nhiên ngắn để các request đang thua cuộc không va lại nhau lần nữa cùng lúc
                    await Task.Delay(Random.Shared.Next(20, 80));
                }
            }

            return Failure("Hệ thống đang bận xử lý thanh toán khác cho hoá đơn này. Vui lòng thử lại.");
        }

        // Deadlock (1205) hoặc xung đột serializable (3960) của SQL Server - EF Core có thể bọc thêm
        // 1-2 lớp exception (DbUpdateException, rồi InvalidOperationException gợi ý bật retry policy)
        // nên phải lần theo toàn bộ chuỗi InnerException thay vì chỉ kiểm tra đúng 1 cấp cố định.
        private static bool IsConcurrencyConflict(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current is SqlException { Number: 1205 or 3960 })
                {
                    return true;
                }
            }

            return false;
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
