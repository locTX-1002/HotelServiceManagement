using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;
namespace Services;

public sealed class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoices; private readonly IPromotionRepository _promotions;
    public InvoiceService() : this(new InvoiceRepository(), new PromotionRepository()) { }
    public InvoiceService(IInvoiceRepository i, IPromotionRepository p) { _invoices = i; _promotions = p; }
    public Task<Invoice?> GetByIdAsync(int id) => _invoices.GetByIdAsync(id); public Task<Invoice?> GetByStayAsync(int id) => _invoices.GetByStayAsync(id);
    public async Task<ServiceResult<Invoice>> PrepareAsync(int stayId, string? promotionCode = null, DateTime? asOf = null)
    {
        if (AppSession.RoleName is not ("Admin" or "Manager" or "Receptionist")) return ServiceResult<Invoice>.Failure("Ban khong co quyen lap hoa don.");
        var stay = await _invoices.GetStayForBillingAsync(stayId); if (stay == null || stay.Status is not (StayStatus.Active or StayStatus.Completed)) return ServiceResult<Invoice>.Failure("Khong tim thay ky luu tru hop le.");
        var invoice = stay.Invoice; var isNew = invoice == null; var paid = invoice?.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount) ?? 0; if (paid > 0) return ServiceResult<Invoice>.Failure("Hoa don da co thanh toan nen khong the tinh lai.");
        var date = asOf ?? stay.ActualCheckOut ?? DateTime.Now; var nights = Math.Max(1, (date.Date - stay.ActualCheckIn.Date).Days); var room = nights * stay.Reservation.Room.RoomType.BasePrice; var services = stay.ServiceOrders.Where(o => o.Status == ServiceOrderStatus.Completed).Sum(o => o.TotalAmount); var surcharge = stay.Surcharges.Sum(x => x.Subtotal); var subtotal = room + services + surcharge; decimal discount = 0; string? applied = null;
        if (!string.IsNullOrWhiteSpace(promotionCode)) { var code = promotionCode.Trim().ToUpperInvariant(); var promo = await _promotions.GetByCodeAsync(code); if (promo == null || !promo.IsActive || date.Date < promo.StartDate.Date || date.Date > promo.EndDate.Date) return ServiceResult<Invoice>.Failure("Ma khuyen mai khong hop le hoac het han."); discount = promo.Type == PromotionType.Percentage ? subtotal * promo.Value / 100m : promo.Value; discount = Math.Clamp(discount, 0, subtotal); applied = promo.Code; }
        invoice ??= new Invoice { StayId = stayId, InvoiceDate = date, CreatedByUserId = AppSession.CurrentUser?.Id }; invoice.RoomCharge = room; invoice.ServiceCharge = services; invoice.SurchargeAmount = surcharge; invoice.DiscountAmount = discount; invoice.PromotionCode = applied; invoice.TotalAmount = subtotal - discount;
        if (isNew && stay.Reservation.DepositAmount is > 0 and var deposit) { if (deposit > invoice.TotalAmount) return ServiceResult<Invoice>.Failure("Tien coc vuot tong hoa don; can xu ly hoan coc truoc."); invoice.Payments.Add(new Payment { PaymentDate = stay.Reservation.DepositPaidAt ?? stay.ActualCheckIn, Amount = deposit, PaymentMethod = stay.Reservation.DepositPaymentMethod ?? PaymentMethod.Cash, Status = PaymentStatus.Completed, TransactionId = $"DEP-{stay.Reservation.BookingCode}", ReceivedByUserId = stay.Reservation.CreatedByUserId }); }
        var paidAmount = invoice.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount); invoice.Status = invoice.TotalAmount <= 0 || paidAmount >= invoice.TotalAmount ? InvoiceStatus.Paid : paidAmount > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Unpaid;
        if (!await _invoices.SaveAsync(invoice, isNew)) return ServiceResult<Invoice>.Failure("Du lieu luu tru/hoa don da thay doi; vui long tai lai."); return ServiceResult<Invoice>.Success(invoice, "Da lap hoa don tam tinh.");
    }
    public async Task<ServiceResult> CancelAsync(int id) { if (AppSession.RoleName is not ("Admin" or "Manager")) return ServiceResult.Failure("Ban khong co quyen huy hoa don."); return await _invoices.CancelAsync(id) ? ServiceResult.Success("Da huy hoa don.") : ServiceResult.Failure("Khong the huy hoa don da co thanh toan."); }
}
