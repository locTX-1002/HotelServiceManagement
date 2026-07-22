using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;

namespace Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    public Task<Invoice?> GetInvoiceAsync(int id) => PaymentDAO.Instance.GetInvoiceAsync(id);
    public Task<List<Payment>> GetByInvoiceAsync(int id) => PaymentDAO.Instance.GetByInvoiceAsync(id);
    public Task<bool> TransactionIdExistsAsync(string id) => PaymentDAO.Instance.TransactionIdExistsAsync(id);
    public Task<Payment?> RecordAsync(int invoiceId, decimal amount, PaymentMethod method,
        string? transactionId, int? userId, DateTime date)
        => PaymentDAO.Instance.RecordAsync(invoiceId, amount, method, transactionId, userId, date);
}
