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
            return ServiceResult<InvoicePaymentSummary>.Failure("Khong tim thay hoa don.");
        var payments = invoice.Payments.OrderByDescending(p => p.PaymentDate).ToList();
        var paid = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
        return ServiceResult<InvoicePaymentSummary>.Success(
            new InvoicePaymentSummary(invoice, paid, Math.Max(0, invoice.TotalAmount - paid), payments));
    }

    public async Task<ServiceResult<Payment>> RecordAsync(int invoiceId, decimal amount,
        PaymentMethod method, string? transactionId)
    {
        if (AppSession.RoleName is not ("Admin" or "Manager" or "Receptionist"))
            return ServiceResult<Payment>.Failure("Ban khong co quyen ghi nhan thanh toan.");
        if (!Enum.IsDefined(method))
            return ServiceResult<Payment>.Failure("Phuong thuc thanh toan khong hop le.");
        if (amount <= 0)
            return ServiceResult<Payment>.Failure("So tien thanh toan phai lon hon 0.");

        var normalizedTransactionId = string.IsNullOrWhiteSpace(transactionId)
            ? null : transactionId.Trim();
        if (method == PaymentMethod.BankTransfer && normalizedTransactionId == null)
            return ServiceResult<Payment>.Failure("Chuyen khoan bat buoc co ma giao dich.");
        if (method == PaymentMethod.Cash && normalizedTransactionId != null)
            return ServiceResult<Payment>.Failure("Thanh toan tien mat khong su dung ma giao dich.");
        if (normalizedTransactionId?.Length > 100)
            return ServiceResult<Payment>.Failure("Ma giao dich toi da 100 ky tu.");
        if (normalizedTransactionId != null
            && await _repository.TransactionIdExistsAsync(normalizedTransactionId))
            return ServiceResult<Payment>.Failure("Ma giao dich da duoc ghi nhan.");

        try
        {
            var payment = await _repository.RecordAsync(invoiceId, amount, method,
                normalizedTransactionId, AppSession.CurrentUser?.Id, DateTime.Now);
            return payment == null
                ? ServiceResult<Payment>.Failure(
                    "Khong the thanh toan: hoa don da dong, ma giao dich trung hoac so tien vuot du no.")
                : ServiceResult<Payment>.Success(payment, "Da ghi nhan thanh toan.");
        }
        catch (DbUpdateException)
        {
            if (normalizedTransactionId != null
                && await _repository.TransactionIdExistsAsync(normalizedTransactionId))
                return ServiceResult<Payment>.Failure("Ma giao dich da duoc ghi nhan.");
            throw;
        }
    }

    public async Task<ServiceResult<Payment>> VoidAsync(int paymentId)
    {
        if (AppSession.RoleName is not ("Admin" or "Manager"))
            return ServiceResult<Payment>.Failure("Ban khong co quyen huy giao dich.");
        var payment = await _repository.VoidAsync(paymentId);
        return payment == null
            ? ServiceResult<Payment>.Failure("Khong tim thay giao dich hoan tat de huy.")
            : ServiceResult<Payment>.Success(payment, "Da huy giao dich va cap nhat lai hoa don.");
    }
}
