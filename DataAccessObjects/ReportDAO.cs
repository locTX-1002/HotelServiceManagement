using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;
namespace DataAccessObjects;

public sealed class ReportDAO
{
    private static readonly Lazy<ReportDAO> LazyInstance = new(() => new ReportDAO()); private ReportDAO() { }
    public static ReportDAO Instance => LazyInstance.Value;
    public async Task<List<Invoice>> GetInvoicesAsync(DateTime from, DateTime toExclusive) { await using var c = HotelDbContextFactory.Create(); return await c.Invoices.AsNoTracking().Where(x => x.Status != InvoiceStatus.Cancelled && x.InvoiceDate >= from && x.InvoiceDate < toExclusive).ToListAsync(); }
    public async Task<List<Payment>> GetPaymentsAsync(DateTime from, DateTime toExclusive) { await using var c = HotelDbContextFactory.Create(); return await c.Payments.AsNoTracking().Where(x => x.Status == PaymentStatus.Completed && x.PaymentDate >= from && x.PaymentDate < toExclusive).ToListAsync(); }
    public async Task<List<Room>> GetRoomsAsync() { await using var c = HotelDbContextFactory.Create(); return await c.Rooms.AsNoTracking().Where(x => x.IsActive).ToListAsync(); }
}
