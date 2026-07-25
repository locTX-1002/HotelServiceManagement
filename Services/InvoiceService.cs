using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;
namespace Services;

public sealed class InvoiceService : IInvoiceService
{
    /// <summary>Ty le giam gia tu dong cho khach VIP (10%) - chinh sach cua khach san.</summary>
    public const decimal VipDiscountRate = 0.10m;

    /// <summary>Nhan ghi vao Invoice.PromotionCode khi hoa don duoc giam vi khach la VIP.</summary>
    public const string VipDiscountLabel = "VIP10";

    private readonly IInvoiceRepository _invoices; private readonly IPromotionRepository _promotions;
    public InvoiceService() : this(new InvoiceRepository(), new PromotionRepository()) { }
    public InvoiceService(IInvoiceRepository i, IPromotionRepository p) { _invoices = i; _promotions = p; }
    public Task<Invoice?> GetByIdAsync(int id) => _invoices.GetByIdAsync(id); public Task<Invoice?> GetByStayAsync(int id) => _invoices.GetByStayAsync(id);
    public async Task<ServiceResult<Invoice>> PrepareAsync(int stayId, string? promotionCode = null, DateTime? asOf = null)
    {
        if (AppSession.RoleName is not ("Admin" or "Manager" or "Receptionist")) return ServiceResult<Invoice>.Failure("Bạn không có quyền lập hoá đơn.");
        var stay = await _invoices.GetStayForBillingAsync(stayId); if (stay == null || stay.Status is not (StayStatus.Active or StayStatus.Completed)) return ServiceResult<Invoice>.Failure("Không tìm thấy lượt lưu trú hợp lệ.");
        var invoice = stay.Invoice; var isNew = invoice == null; var paid = invoice?.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount) ?? 0;
        var date = asOf ?? stay.ActualCheckOut ?? DateTime.Now; var nights = Math.Max(1, (date.Date - stay.ActualCheckIn.Date).Days); var room = nights * stay.Reservation.Room.RoomType.BasePrice; var services = stay.ServiceOrders.Where(o => o.Status == ServiceOrderStatus.Completed).Sum(o => o.TotalAmount); var surcharge = stay.Surcharges.Sum(x => x.Subtotal); var subtotal = room + services + surcharge; decimal discount = 0; string? applied = null;
        if (!string.IsNullOrWhiteSpace(promotionCode)) { var code = promotionCode.Trim().ToUpperInvariant(); var promo = await _promotions.GetByCodeAsync(code); if (promo == null || !promo.IsActive || date.Date < promo.StartDate.Date || date.Date > promo.EndDate.Date) return ServiceResult<Invoice>.Failure("Mã khuyến mãi không hợp lệ hoặc hết hạn."); discount = promo.Type == PromotionType.Percentage ? subtotal * promo.Value / 100m : promo.Value; applied = promo.Code; }

        // Uu dai khach VIP: tu dong giam 10% tren tong tam tinh, khong can nhap ma.
        // Cong DON voi ma khuyen mai (neu co) roi moi clamp mot lan o duoi, nen tong giam
        // khong bao gio vuot qua so tien phai tra.
        var vipDiscount = stay.Reservation.Guest?.Tag == GuestTag.Vip ? subtotal * VipDiscountRate : 0m;
        discount = Math.Clamp(discount + vipDiscount, 0, subtotal);

        // Ghi ro ly do giam vao PromotionCode de le tan/khach doc duoc tren hoa don.
        // Dung lai cot san co thay vi them cot moi - nhom cam tu tao migration.
        if (vipDiscount > 0) { applied = applied == null ? VipDiscountLabel : $"{applied}+{VipDiscountLabel}"; }
        // Truoc day he chan thang moi hoa don da co thanh toan la khong tinh lai duoc.
        // Chan nhu vay lam mat tien: khach goi dich vu SAU khi le tan lap hoa don thi
        // khoan do khong bao gio vao duoc bill. Gio chi chan dung truong hop tong moi
        // TUT XUONG duoi so da thu - do la phai hoan tien, xu ly tay ngoai pham vi app.
        // Tong tang len thi cho tinh lai binh thuong, hoa don ve PartiallyPaid va le tan
        // thu not phan chenh bang luong thanh toan san co.
        var total = subtotal - discount;
        if (!isNew && total < paid) return ServiceResult<Invoice>.Failure($"Tổng mới ({total:N0} đ) thấp hơn số đã thu ({paid:N0} đ); cần xử lý hoàn tiền trước.");
        invoice ??= new Invoice { StayId = stayId, InvoiceDate = date, CreatedByUserId = AppSession.CurrentUser?.Id }; invoice.RoomCharge = room; invoice.ServiceCharge = services; invoice.SurchargeAmount = surcharge; invoice.DiscountAmount = discount; invoice.PromotionCode = applied; invoice.TotalAmount = total;
        if (isNew && stay.Reservation.DepositAmount is > 0 and var deposit) { if (deposit > invoice.TotalAmount) return ServiceResult<Invoice>.Failure("Tiền cọc vượt tổng hoá đơn; cần xử lý hoàn cọc trước."); invoice.Payments.Add(new Payment { PaymentDate = stay.Reservation.DepositPaidAt ?? stay.ActualCheckIn, Amount = deposit, PaymentMethod = stay.Reservation.DepositPaymentMethod ?? PaymentMethod.Cash, Status = PaymentStatus.Completed, TransactionId = $"DEP-{stay.Reservation.BookingCode}", ReceivedByUserId = stay.Reservation.CreatedByUserId }); }
        var paidAmount = invoice.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount); invoice.Status = invoice.TotalAmount <= 0 || paidAmount >= invoice.TotalAmount ? InvoiceStatus.Paid : paidAmount > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Unpaid;
        if (!await _invoices.SaveAsync(invoice, isNew)) return ServiceResult<Invoice>.Failure("Dữ liệu lưu trú hoặc hoá đơn đã thay đổi; vui lòng tải lại."); return ServiceResult<Invoice>.Success(invoice, "Đã lập hoá đơn tạm tính.");
    }
    public async Task<ServiceResult> CancelAsync(int id) { if (AppSession.RoleName is not ("Admin" or "Manager")) return ServiceResult.Failure("Bạn không có quyền huỷ hoá đơn."); return await _invoices.CancelAsync(id) ? ServiceResult.Success("Đã huỷ hoá đơn.") : ServiceResult.Failure("Không huỷ được hoá đơn đã có thanh toán."); }
}
