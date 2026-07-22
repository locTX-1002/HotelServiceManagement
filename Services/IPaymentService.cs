using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Services;

public record InvoicePaymentSummary(
    Invoice Invoice,
    decimal PaidAmount,
    decimal RemainingAmount,
    IReadOnlyList<Payment> Payments);

public interface IPaymentService
{
    Task<ServiceResult<InvoicePaymentSummary>> GetSummaryAsync(int invoiceId);
    Task<ServiceResult<Payment>> RecordAsync(int invoiceId, decimal amount,
        PaymentMethod method, string? transactionId);
}
