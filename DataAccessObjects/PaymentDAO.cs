using System.Data;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

/// <summary>DAO Singleton ghi nhan thanh toan va cap nhat hoa don trong mot transaction.</summary>
public sealed class PaymentDAO
{
    private static readonly Lazy<PaymentDAO> LazyInstance = new(() => new PaymentDAO());
    private PaymentDAO() { }
    public static PaymentDAO Instance => LazyInstance.Value;

    public async Task<Invoice?> GetInvoiceAsync(int invoiceId)
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Invoices.AsNoTracking()
            .Include(i => i.Payments)
            .Include(i => i.Stay).ThenInclude(s => s.Reservation).ThenInclude(r => r.Guest)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    public async Task<List<Payment>> GetByInvoiceAsync(int invoiceId)
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Payments.AsNoTracking()
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<bool> TransactionIdExistsAsync(string transactionId)
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Payments.AnyAsync(p => p.TransactionId == transactionId);
    }

    /// <summary>
    /// Tra ve null neu hoa don khong con hop le, so tien vuot du no hoac ma giao dich bi trung.
    /// Serializable bao ve hai may le tan thanh toan dong thoi cho cung mot hoa don.
    /// </summary>
    public async Task<Payment?> RecordAsync(int invoiceId, decimal amount, PaymentMethod method,
        string? transactionId, int? receivedByUserId, DateTime paymentDate)
    {
        await using var context = HotelDbContextFactory.Create();
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var invoice = await context.Invoices.Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice == null || invoice.Status == InvoiceStatus.Cancelled
            || invoice.Status == InvoiceStatus.Paid)
            return null;

        if (transactionId != null
            && await context.Payments.AnyAsync(p => p.TransactionId == transactionId))
            return null;

        var paid = invoice.Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .Sum(p => p.Amount);
        var remaining = invoice.TotalAmount - paid;
        if (amount <= 0 || amount > remaining) return null;

        var payment = new Payment
        {
            InvoiceId = invoiceId,
            PaymentDate = paymentDate,
            Amount = amount,
            PaymentMethod = method,
            Status = PaymentStatus.Completed,
            TransactionId = transactionId,
            ReceivedByUserId = receivedByUserId,
        };
        context.Payments.Add(payment);

        var newPaidTotal = paid + amount;
        invoice.Status = newPaidTotal >= invoice.TotalAmount
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return payment;
    }

    public async Task<Payment?> VoidAsync(int paymentId)
    {
        await using var context = HotelDbContextFactory.Create();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var payment = await context.Payments.Include(p => p.Invoice).ThenInclude(i => i.Payments)
            .FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment == null || payment.Status != PaymentStatus.Completed) return null;

        payment.Status = PaymentStatus.Cancelled;
        var remainingPaid = payment.Invoice.Payments
            .Where(p => p.Id != paymentId && p.Status == PaymentStatus.Completed)
            .Sum(p => p.Amount);
        payment.Invoice.Status = remainingPaid <= 0
            ? InvoiceStatus.Unpaid
            : remainingPaid >= payment.Invoice.TotalAmount
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return payment;
    }
}
