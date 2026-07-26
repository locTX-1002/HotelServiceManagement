using BusinessObjects;
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

    /// <summary>
    /// Nhan ghi vao Invoice.PromotionCode khi quan ly tu go so tien giam, khong qua ma
    /// khuyen mai nao. De nhan NGAN vi cot chi 30 ky tu va con phai cho ma + VIP10 dung
    /// chung o cung o (vi du "HE2026+VIP10+TUNHAP" = 19 ky tu).
    /// </summary>
    public const string ManualDiscountLabel = "TUNHAP";

    private readonly IInvoiceRepository _invoices; private readonly IPromotionRepository _promotions;
    public InvoiceService() : this(new InvoiceRepository(), new PromotionRepository()) { }
    public InvoiceService(IInvoiceRepository i, IPromotionRepository p) { _invoices = i; _promotions = p; }
    public Task<Invoice?> GetByIdAsync(int id) => _invoices.GetByIdAsync(id); public Task<Invoice?> GetByStayAsync(int id) => _invoices.GetByStayAsync(id);
    public async Task<ServiceResult<Invoice>> PrepareAsync(int stayId, string? promotionCode = null, DateTime? asOf = null, decimal manualDiscount = 0)
    {
        if (manualDiscount < 0) return ServiceResult<Invoice>.Failure("Số tiền giảm tay không được âm.");
        if (manualDiscount > 0 && !AuthorizationPolicy.CanGiveManualDiscount) return ServiceResult<Invoice>.Failure("Chỉ Quản trị viên hoặc Quản lý mới được giảm giá tay.");
        if (AppSession.RoleName is not (RoleNames.Admin or RoleNames.Manager or RoleNames.Receptionist)) return ServiceResult<Invoice>.Failure("Bạn không có quyền lập hoá đơn.");
        var stay = await _invoices.GetStayForBillingAsync(stayId); if (stay == null || stay.Status is not (StayStatus.Active or StayStatus.Completed)) return ServiceResult<Invoice>.Failure("Không tìm thấy lượt lưu trú hợp lệ.");
        var invoice = stay.Invoice; var isNew = invoice == null; var paid = invoice?.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount) ?? 0;
        var date = asOf ?? stay.ActualCheckOut ?? DateTime.Now;
        var nights = BillingRules.ChargeableNights(stay.ActualCheckIn,
            stay.Reservation.CheckInDate, stay.Reservation.CheckOutDate, date); var room = nights * stay.Reservation.Room.RoomType.BasePrice; var services = stay.ServiceOrders.Where(o => o.Status == ServiceOrderStatus.Completed).Sum(o => o.TotalAmount); var surcharge = stay.Surcharges.Sum(x => x.Subtotal); var subtotal = room + services + surcharge; decimal discount = 0; string? applied = null;
        // GIAM GIA CHOT TAI LUC LAP HOA DON. Khi hoa don da thu tien, giu nguyen muc giam
        // va nhan giam cu thay vi tinh lai theo the VIP / ma khuyen mai hien tai. Neu khong,
        // chi can gan the VIP cho khach sau khi ho da tra tien la tong tut xuong duoi so da
        // thu, va luc do khong tinh lai duoc ma cung khong tra phong duoc - le tan ket cung.
        var frozen = !isNew && paid > 0;
        if (frozen)
        {
            discount = invoice!.DiscountAmount; applied = invoice.PromotionCode;
            discount = Math.Clamp(discount, 0, subtotal);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(promotionCode))
            {
                var code = promotionCode.Trim().ToUpperInvariant();
                var promo = await _promotions.GetByCodeAsync(code);
                // Tach ba ly do thay vi gop mot cau: le tan doc "khong hop le hoac het han"
                // thi khong biet minh go sai ma hay ma da het han, phai mo man Khuyen mai ra
                // do tay. Noi thang ra thi biet phai lam gi.
                if (promo == null) return ServiceResult<Invoice>.Failure($"Không tìm thấy mã khuyến mãi \"{code}\".");
                if (!promo.IsActive) return ServiceResult<Invoice>.Failure($"Mã \"{promo.Code}\" đang tắt, không dùng được.");
                if (date.Date < promo.StartDate.Date) return ServiceResult<Invoice>.Failure($"Mã \"{promo.Code}\" chưa tới ngày áp dụng (từ {promo.StartDate:dd/MM/yyyy}).");
                if (date.Date > promo.EndDate.Date) return ServiceResult<Invoice>.Failure($"Mã \"{promo.Code}\" đã hết hạn ngày {promo.EndDate:dd/MM/yyyy}.");
                discount = promo.Type == PromotionType.Percentage ? subtotal * promo.Value / 100m : promo.Value;
                applied = promo.Code;
            }

            // Uu dai khach VIP: tu dong giam 10% tren tong tam tinh, khong can nhap ma.
            // Cong DON voi ma khuyen mai (neu co) roi moi clamp mot lan o duoi, nen tong giam
            // khong bao gio vuot qua so tien phai tra.
            var vipDiscount = stay.Reservation.Guest?.Tag == GuestTag.Vip ? subtotal * VipDiscountRate : 0m;

            // Giam TU NHAP: quan ly go thang so tien, khong qua ma nao. Cong don cung
            // ma khuyen mai va uu dai VIP roi clamp mot lan o duoi, nen du go so to hon
            // ca hoa don cung chi giam toi 0 chu khong ra tong am.
            discount = Math.Clamp(discount + vipDiscount + manualDiscount, 0, subtotal);

            // Ghi ro ly do giam vao PromotionCode de le tan/khach doc duoc tren hoa don.
            // Dung lai cot san co thay vi them cot moi - nhom cam tu tao migration.
            if (vipDiscount > 0) { applied = applied == null ? VipDiscountLabel : $"{applied}+{VipDiscountLabel}"; }
            if (manualDiscount > 0) { applied = applied == null ? ManualDiscountLabel : $"{applied}+{ManualDiscountLabel}"; }
        }
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
        if (!await _invoices.SaveAsync(invoice, isNew)) return ServiceResult<Invoice>.Failure("Dữ liệu lưu trú hoặc hoá đơn đã thay đổi; vui lòng tải lại.");
        return ServiceResult<Invoice>.Success(invoice, isNew ? "Đã lập hoá đơn tạm tính."
            : frozen ? "Đã cập nhật hoá đơn. Giảm giá giữ nguyên vì hoá đơn đã thu tiền."
            : "Đã tính lại hoá đơn.");
    }
    public async Task<ServiceResult> CancelAsync(int id) { if (AppSession.RoleName is not (RoleNames.Admin or RoleNames.Manager)) return ServiceResult.Failure("Bạn không có quyền huỷ hoá đơn."); return await _invoices.CancelAsync(id) ? ServiceResult.Success("Đã huỷ hoá đơn.") : ServiceResult.Failure("Không huỷ được hoá đơn đã có thanh toán."); }
}
