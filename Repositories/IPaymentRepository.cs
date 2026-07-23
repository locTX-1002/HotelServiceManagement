using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Repositories;

public interface IPaymentRepository
{
    Task<Invoice?> GetInvoiceAsync(int invoiceId);
    Task<List<Payment>> GetByInvoiceAsync(int invoiceId);
    Task<bool> TransactionIdExistsAsync(string transactionId);
    Task<Payment?> RecordAsync(int invoiceId, decimal amount, PaymentMethod method,
        string? transactionId, int? receivedByUserId, DateTime paymentDate);
    Task<Payment?> VoidAsync(int paymentId);
}
