using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;
using Repositories;

namespace Services;

public sealed class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    public PaymentService() : this(new PaymentRepository()) { }
    public PaymentService(IPaymentRepository repository) => _repository = repository;

    public async Task<ServiceResult<InvoicePaymentSummary>> GetSummaryAsync(int invoiceId)
    {
        var invoice = await _repository.GetInvoiceAsync(invoiceId);
        if (invoice == null)
            return ServiceResult<InvoicePaymentSummary>.Failure("Không tìm thấy hoá đơn.");
        var payments = invoice.Payments.OrderByDescending(p => p.PaymentDate).ToList();
        var paid = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
        return ServiceResult<InvoicePaymentSummary>.Success(
            new InvoicePaymentSummary(invoice, paid, Math.Max(0, invoice.TotalAmount - paid), payments));
    }

    public async Task<ServiceResult<Payment>> RecordAsync(int invoiceId, decimal amount,
        PaymentMethod method, string? transactionId)
    {
        if (AppSession.RoleName is not (RoleNames.Manager or RoleNames.Receptionist))
            return ServiceResult<Payment>.Failure("Bạn không có quyền ghi nhận thanh toán.");
        if (!Enum.IsDefined(method))
            return ServiceResult<Payment>.Failure("Phương thức thanh toán không hợp lệ.");
        if (amount <= 0)
            return ServiceResult<Payment>.Failure("Số tiền thanh toán phải lớn hơn 0.");

        var normalizedTransactionId = string.IsNullOrWhiteSpace(transactionId)
            ? null : transactionId.Trim();
        if (method == PaymentMethod.BankTransfer && normalizedTransactionId == null)
            return ServiceResult<Payment>.Failure("Chuyển khoản bắt buộc có mã giao dịch.");
        if (method == PaymentMethod.Cash && normalizedTransactionId != null)
            return ServiceResult<Payment>.Failure("Thanh toán tiền mặt không dùng mã giao dịch.");
        if (normalizedTransactionId?.Length > 100)
            return ServiceResult<Payment>.Failure("Mã giao dịch tối đa 100 ký tự.");
        if (normalizedTransactionId != null
            && await _repository.TransactionIdExistsAsync(normalizedTransactionId))
            return ServiceResult<Payment>.Failure("Mã giao dịch đã được ghi nhận.");

        try
        {
            var payment = await _repository.RecordAsync(invoiceId, amount, method,
                normalizedTransactionId, AppSession.CurrentUser?.Id, DateTime.Now);
            return payment == null
                ? ServiceResult<Payment>.Failure(
                    "Không thanh toán được: hoá đơn đã đóng, mã giao dịch trùng, hoặc số tiền vượt quá số còn nợ.")
                : ServiceResult<Payment>.Success(payment, "Đã ghi nhận thanh toán.");
        }
        catch (DbUpdateException)
        {
            if (normalizedTransactionId != null
                && await _repository.TransactionIdExistsAsync(normalizedTransactionId))
                return ServiceResult<Payment>.Failure("Mã giao dịch đã được ghi nhận.");
            throw;
        }
    }

    public async Task<ServiceResult<Payment>> VoidAsync(int paymentId)
    {
        if (AppSession.RoleName is not RoleNames.Manager)
            return ServiceResult<Payment>.Failure("Bạn không có quyền huỷ giao dịch.");
        var payment = await _repository.VoidAsync(paymentId);
        return payment == null
            ? ServiceResult<Payment>.Failure("Không tìm thấy giao dịch đã hoàn tất để huỷ.")
            : ServiceResult<Payment>.Success(payment, "Đã huỷ giao dịch và cập nhật lại hoá đơn.");
    }
}
