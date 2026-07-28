using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DataAccessObjects;

public sealed class InvoiceDAO
{
    private static readonly Lazy<InvoiceDAO> LazyInstance = new(() => new InvoiceDAO());

    private InvoiceDAO() { }

    public static InvoiceDAO Instance => LazyInstance.Value;

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        await using var context = HotelDbContextFactory.Create();
        return await InvoiceQuery(context).FirstOrDefaultAsync(invoice => invoice.Id == id);
    }

    public async Task<Invoice?> GetByStayAsync(int id)
    {
        await using var context = HotelDbContextFactory.Create();
        return await InvoiceQuery(context).FirstOrDefaultAsync(invoice => invoice.StayId == id);
    }

    public async Task<Stay?> GetStayForBillingAsync(int id)
    {
        await using var context = HotelDbContextFactory.Create();

        return await context.Stays
            .AsNoTracking()
            .AsSplitQuery()
            .Include(stay => stay.Reservation)
                .ThenInclude(reservation => reservation.Room)
                .ThenInclude(room => room.RoomType)
            .Include(stay => stay.Reservation)
                .ThenInclude(reservation => reservation.Guest)
            .Include(stay => stay.ServiceOrders)
            .Include(stay => stay.Surcharges)
            .Include(stay => stay.Invoice)
                .ThenInclude(invoice => invoice!.Payments)
            .Include(stay => stay.Invoice)
                .ThenInclude(invoice => invoice!.Promotion)
            .FirstOrDefaultAsync(stay => stay.Id == id);
    }

    public async Task<bool> SaveAsync(Invoice invoice, bool add)
    {
        await using var context = HotelDbContextFactory.Create();
        await using var localTransaction = context.Database.CurrentTransaction == null
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        var stayIsBillable = await context.Stays.AnyAsync(stay =>
            stay.Id == invoice.StayId &&
            (stay.Status == StayStatus.Active || stay.Status == StayStatus.Completed));

        if (!stayIsBillable)
        {
            return false;
        }

        if (add)
        {
            if (await context.Invoices.AnyAsync(item => item.StayId == invoice.StayId))
            {
                return false;
            }

            // Chỉ lưu FK; không attach object Promotion/Stay được lấy bằng AsNoTracking.
            invoice.Stay = null!;
            invoice.Promotion = null;
            context.Invoices.Add(invoice);
        }
        else
        {
            var current = await context.Invoices
                .Include(item => item.Payments)
                .FirstOrDefaultAsync(item =>
                    item.Id == invoice.Id &&
                    item.StayId == invoice.StayId);

            if (current == null)
            {
                return false;
            }

            var paid = current.Payments
                .Where(payment => payment.Status == PaymentStatus.Completed)
                .Sum(payment => payment.Amount);

            if (invoice.TotalAmount < paid)
            {
                return false;
            }

            current.InvoiceDate = invoice.InvoiceDate;
            current.RoomCharge = invoice.RoomCharge;
            current.ServiceCharge = invoice.ServiceCharge;
            current.SurchargeAmount = invoice.SurchargeAmount;
            current.DiscountAmount = invoice.DiscountAmount;
            current.PromotionId = invoice.PromotionId;
            current.PromotionCode = invoice.PromotionCode;
            current.ManualDiscountAmount = invoice.ManualDiscountAmount;
            current.IsVipDiscountApplied = invoice.IsVipDiscountApplied;
            current.TotalAmount = invoice.TotalAmount;
            current.Status = invoice.Status;
        }

        await context.SaveChangesAsync();

        if (localTransaction != null)
        {
            await localTransaction.CommitAsync();
        }

        return true;
    }

    public async Task<bool> CancelAsync(int id)
    {
        await using var context = HotelDbContextFactory.Create();
        var invoice = await context.Invoices
            .Include(item => item.Payments)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (invoice == null ||
            invoice.Payments.Any(payment => payment.Status == PaymentStatus.Completed))
        {
            return false;
        }

        invoice.Status = InvoiceStatus.Cancelled;
        await context.SaveChangesAsync();
        return true;
    }

    private static IQueryable<Invoice> InvoiceQuery(HotelDbContext context)
        => context.Invoices
            .AsNoTracking()
            .AsSplitQuery()
            .Include(invoice => invoice.Payments)
            .Include(invoice => invoice.CreatedByUser)
            .Include(invoice => invoice.Promotion)
            .Include(invoice => invoice.Stay)
                .ThenInclude(stay => stay.Reservation)
                .ThenInclude(reservation => reservation.Guest)
            .Include(invoice => invoice.Stay)
                .ThenInclude(stay => stay.Reservation)
                .ThenInclude(reservation => reservation.Room)
                .ThenInclude(room => room.RoomType)
            .Include(invoice => invoice.Stay)
                .ThenInclude(stay => stay.Surcharges)
                .ThenInclude(surcharge => surcharge.SurchargeItem);
}
