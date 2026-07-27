using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace DataAccessObjects;

public sealed class InvoiceDAO
{
    private static readonly Lazy<InvoiceDAO> LazyInstance = new(() => new InvoiceDAO()); private InvoiceDAO() { }
    public static InvoiceDAO Instance => LazyInstance.Value;
    public async Task<Invoice?> GetByIdAsync(int id) { await using var c = HotelDbContextFactory.Create(); return await InvoiceQuery(c).FirstOrDefaultAsync(x => x.Id == id); }
    public async Task<Invoice?> GetByStayAsync(int id) { await using var c = HotelDbContextFactory.Create(); return await InvoiceQuery(c).FirstOrDefaultAsync(x => x.StayId == id); }
    // Include ca Guest vi hoa don can biet khach co phai VIP khong (VIP duoc giam 10%).
    public async Task<Stay?> GetStayForBillingAsync(int id) { await using var c = HotelDbContextFactory.Create(); return await c.Stays.AsNoTracking().AsSplitQuery().Include(s => s.Reservation).ThenInclude(r => r.Room).ThenInclude(r => r.RoomType).Include(s => s.Reservation).ThenInclude(r => r.Guest).Include(s => s.ServiceOrders).Include(s => s.Surcharges).Include(s => s.Invoice).ThenInclude(i => i!.Payments).FirstOrDefaultAsync(s => s.Id == id); }
    public async Task<bool> SaveAsync(Invoice invoice, bool add)
    {
        await using var c = HotelDbContextFactory.Create();
        await using var localTransaction = c.Database.CurrentTransaction == null
            ? await c.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var stayIsBillable = await c.Stays.AnyAsync(s => s.Id == invoice.StayId
            && (s.Status == StayStatus.Active || s.Status == StayStatus.Completed));
        if (!stayIsBillable) return false;

        if (add)
        {
            if (await c.Invoices.AnyAsync(i => i.StayId == invoice.StayId)) return false;
            invoice.Stay = null!;
            c.Invoices.Add(invoice);
        }
        else
        {
            var current = await c.Invoices.Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == invoice.Id && i.StayId == invoice.StayId);
            if (current == null) return false;
            // Hoa don da thu tien van cho sua, mien la tong moi khong tut xuong duoi
            // so da thu (truong hop do phai hoan tien, khong lam trong app). Nho vay
            // dich vu khach goi them sau khi lap hoa don moi vao duoc bill.
            if (invoice.TotalAmount < current.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount)) return false;
            current.InvoiceDate = invoice.InvoiceDate;
            current.RoomCharge = invoice.RoomCharge;
            current.ServiceCharge = invoice.ServiceCharge;
            current.SurchargeAmount = invoice.SurchargeAmount;
            current.DiscountAmount = invoice.DiscountAmount;
            current.PromotionCode = invoice.PromotionCode;
            current.TotalAmount = invoice.TotalAmount;
            current.Status = invoice.Status;
        }

        await c.SaveChangesAsync();
        if (localTransaction != null)
        {
            await localTransaction.CommitAsync();
        }
        return true;
    }
    public async Task<bool> CancelAsync(int id) { await using var c = HotelDbContextFactory.Create(); var x = await c.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == id); if (x == null || x.Payments.Any(p => p.Status == PaymentStatus.Completed)) return false; x.Status = InvoiceStatus.Cancelled; await c.SaveChangesAsync(); return true; }
    private static IQueryable<Invoice> InvoiceQuery(HotelDbContext c) => c.Invoices.AsNoTracking().AsSplitQuery().Include(i => i.Payments).Include(i => i.CreatedByUser).Include(i => i.Stay).ThenInclude(s => s.Reservation).ThenInclude(r => r.Guest).Include(i => i.Stay).ThenInclude(s => s.Reservation).ThenInclude(r => r.Room).ThenInclude(r => r.RoomType).Include(i => i.Stay).ThenInclude(s => s.Surcharges).ThenInclude(s => s.SurchargeItem);
}
