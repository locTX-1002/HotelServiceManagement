using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;

namespace Services;

public sealed class InvoiceService : IInvoiceService
{
    /// <summary>Tỷ lệ giảm giá tự động cho khách VIP (10%).</summary>
    public const decimal VipDiscountRate = 0.10m;

    private readonly IInvoiceRepository _invoices;
    private readonly IPromotionRepository _promotions;

    public InvoiceService() : this(new InvoiceRepository(), new PromotionRepository()) { }

    public InvoiceService(IInvoiceRepository invoices, IPromotionRepository promotions)
    {
        _invoices = invoices;
        _promotions = promotions;
    }

    public Task<Invoice?> GetByIdAsync(int id) => _invoices.GetByIdAsync(id);

    public Task<Invoice?> GetByStayAsync(int id) => _invoices.GetByStayAsync(id);

    public Task<ServiceResult<Invoice>> PrepareAsync(
        int stayId,
        string? promotionCode = null,
        DateTime? asOf = null,
        decimal? manualDiscount = null)
        => PrepareCoreAsync(
            stayId,
            promotionCode,
            asOf,
            manualDiscount,
            preserveExistingPromotion: false,
            requirePreparePermission: true);

    public Task<ServiceResult<Invoice>> ApplyApprovedDiscountAsync(
        int stayId,
        decimal manualDiscount)
    {
        if (!AuthorizationPolicy.CanGiveManualDiscount)
        {
            return Task.FromResult(
                ServiceResult<Invoice>.Failure("Bạn không có quyền duyệt giảm giá."));
        }

        // Khi quản lý duyệt giảm tay, phải giữ nguyên mã khuyến mãi thật đã chọn trước đó.
        return PrepareCoreAsync(
            stayId,
            promotionCode: null,
            asOf: null,
            manualDiscountOverride: manualDiscount,
            preserveExistingPromotion: true,
            requirePreparePermission: false);
    }

    private async Task<ServiceResult<Invoice>> PrepareCoreAsync(
        int stayId,
        string? promotionCode,
        DateTime? asOf,
        decimal? manualDiscountOverride,
        bool preserveExistingPromotion,
        bool requirePreparePermission)
    {
        if (manualDiscountOverride is < 0)
        {
            return ServiceResult<Invoice>.Failure("Số tiền giảm tay không được âm.");
        }

        if (manualDiscountOverride is > 0 && !AuthorizationPolicy.CanGiveManualDiscount)
        {
            return ServiceResult<Invoice>.Failure(
                "Chỉ Quản trị viên hoặc Quản lý mới được giảm giá tay.");
        }

        if (requirePreparePermission && !AuthorizationPolicy.CanPrepareInvoice)
        {
            return ServiceResult<Invoice>.Failure("Bạn không có quyền lập hoá đơn.");
        }

        var stay = await _invoices.GetStayForBillingAsync(stayId);
        if (stay == null || stay.Status is not (StayStatus.Active or StayStatus.Completed))
        {
            return ServiceResult<Invoice>.Failure("Không tìm thấy lượt lưu trú hợp lệ.");
        }

        var invoice = stay.Invoice;
        var isNew = invoice == null;
        var paid = invoice?.Payments
            .Where(payment => payment.Status == PaymentStatus.Completed)
            .Sum(payment => payment.Amount) ?? 0m;

        var date = asOf ?? stay.ActualCheckOut ?? DateTime.Now;
        var nights = BillingRules.ChargeableNights(stay.ActualCheckIn, date);
        var roomCharge = nights * stay.Reservation.Room.RoomType.BasePrice;
        var serviceCharge = stay.ServiceOrders
            .Where(order => order.Status == ServiceOrderStatus.Completed)
            .Sum(order => order.TotalAmount);
        var surchargeAmount = stay.Surcharges.Sum(item => item.Subtotal);
        var subtotal = roomCharge + serviceCharge + surchargeAmount;

        decimal discountAmount;
        decimal manualDiscountAmount;
        bool isVipDiscountApplied;
        int? promotionId;
        string? appliedPromotionCode;

        // Hóa đơn đã thu tiền: giữ nguyên toàn bộ cấu hình giảm giá đã chốt.
        // Chỉ cập nhật tiền phòng/dịch vụ/phụ thu và chặn tổng mới thấp hơn số đã thu.
        var frozen = !isNew && paid > 0;
        if (frozen)
        {
            discountAmount = Math.Clamp(invoice!.DiscountAmount, 0m, subtotal);
            manualDiscountAmount = invoice.ManualDiscountAmount;
            isVipDiscountApplied = invoice.IsVipDiscountApplied;
            promotionId = invoice.PromotionId;
            appliedPromotionCode = invoice.PromotionCode;
        }
        else
        {
            Promotion? promotion = null;

            if (preserveExistingPromotion)
            {
                if (invoice?.PromotionId is int existingPromotionId)
                {
                    promotion = await _promotions.GetByIdAsync(existingPromotionId);
                }
                else if (!string.IsNullOrWhiteSpace(invoice?.PromotionCode))
                {
                    promotion = await _promotions.GetByCodeAsync(
                        invoice.PromotionCode.Trim().ToUpperInvariant());
                }
            }
            else if (!string.IsNullOrWhiteSpace(promotionCode))
            {
                promotion = await _promotions.GetByCodeAsync(
                    promotionCode.Trim().ToUpperInvariant());
            }

            if (promotion != null)
            {
                var validationError = ValidatePromotion(promotion, date);
                if (validationError != null)
                {
                    return ServiceResult<Invoice>.Failure(validationError);
                }
            }
            else if (!preserveExistingPromotion &&
                     !string.IsNullOrWhiteSpace(promotionCode))
            {
                var code = promotionCode.Trim().ToUpperInvariant();
                return ServiceResult<Invoice>.Failure(
                    $"Không tìm thấy mã khuyến mãi \"{code}\".");
            }

            var promotionDiscount = promotion == null
                ? 0m
                : promotion.Type == PromotionType.Percentage
                    ? subtotal * promotion.Value / 100m
                    : promotion.Value;

            isVipDiscountApplied = stay.Reservation.Guest?.Tag == GuestTag.Vip;
            var vipDiscount = isVipDiscountApplied
                ? subtotal * VipDiscountRate
                : 0m;

            // null nghĩa là tính lại và giữ số giảm tay đã được quản lý duyệt trước đó.
            var requestedManualDiscount =
                manualDiscountOverride ?? invoice?.ManualDiscountAmount ?? 0m;

            manualDiscountAmount = Math.Clamp(
                requestedManualDiscount,
                0m,
                subtotal);

            discountAmount = Math.Clamp(
                promotionDiscount + vipDiscount + manualDiscountAmount,
                0m,
                subtotal);

            promotionId = promotion?.Id;
            appliedPromotionCode = promotion?.Code;
        }

        var totalAmount = subtotal - discountAmount;
        if (!isNew && totalAmount < paid)
        {
            return ServiceResult<Invoice>.Failure(
                $"Tổng mới ({totalAmount:N0} đ) thấp hơn số đã thu ({paid:N0} đ); " +
                "cần xử lý hoàn tiền trước.");
        }

        invoice ??= new Invoice
        {
            StayId = stayId,
            InvoiceDate = date,
            CreatedByUserId = AppSession.CurrentUser?.Id,
        };

        invoice.RoomCharge = roomCharge;
        invoice.ServiceCharge = serviceCharge;
        invoice.SurchargeAmount = surchargeAmount;
        invoice.DiscountAmount = discountAmount;
        invoice.PromotionId = promotionId;
        invoice.PromotionCode = appliedPromotionCode;
        invoice.ManualDiscountAmount = manualDiscountAmount;
        invoice.IsVipDiscountApplied = isVipDiscountApplied;
        invoice.TotalAmount = totalAmount;

        if (isNew && stay.Reservation.DepositAmount is > 0 and var deposit)
        {
            if (deposit > invoice.TotalAmount)
            {
                return ServiceResult<Invoice>.Failure(
                    "Tiền cọc vượt tổng hoá đơn; cần xử lý hoàn cọc trước.");
            }

            invoice.Payments.Add(new Payment
            {
                PaymentDate = stay.Reservation.DepositPaidAt ?? stay.ActualCheckIn,
                Amount = deposit,
                PaymentMethod = stay.Reservation.DepositPaymentMethod ?? PaymentMethod.Cash,
                Status = PaymentStatus.Completed,
                TransactionId = $"DEP-{stay.Reservation.BookingCode}",
                ReceivedByUserId = stay.Reservation.CreatedByUserId,
            });
        }

        var paidAmount = invoice.Payments
            .Where(payment => payment.Status == PaymentStatus.Completed)
            .Sum(payment => payment.Amount);

        invoice.Status = invoice.TotalAmount <= 0m || paidAmount >= invoice.TotalAmount
            ? InvoiceStatus.Paid
            : paidAmount > 0m
                ? InvoiceStatus.PartiallyPaid
                : InvoiceStatus.Unpaid;

        if (!await _invoices.SaveAsync(invoice, isNew))
        {
            return ServiceResult<Invoice>.Failure(
                "Dữ liệu lưu trú hoặc hoá đơn đã thay đổi; vui lòng tải lại.");
        }

        var message = isNew
            ? "Đã lập hoá đơn tạm tính."
            : frozen
                ? "Đã cập nhật hoá đơn. Giảm giá giữ nguyên vì hoá đơn đã thu tiền."
                : "Đã tính lại hoá đơn.";

        return ServiceResult<Invoice>.Success(invoice, message);
    }

    private static string? ValidatePromotion(Promotion promotion, DateTime date)
    {
        if (!promotion.IsActive)
        {
            return $"Mã \"{promotion.Code}\" đang tắt, không dùng được.";
        }

        if (date.Date < promotion.StartDate.Date)
        {
            return $"Mã \"{promotion.Code}\" chưa tới ngày áp dụng " +
                   $"(từ {promotion.StartDate:dd/MM/yyyy}).";
        }

        if (date.Date > promotion.EndDate.Date)
        {
            return $"Mã \"{promotion.Code}\" đã hết hạn ngày " +
                   $"{promotion.EndDate:dd/MM/yyyy}.";
        }

        return null;
    }

    public async Task<ServiceResult> CancelAsync(int id)
    {
        if (!AuthorizationPolicy.CanApproveInvoiceCancel)
        {
            return ServiceResult.Failure("Bạn không có quyền duyệt huỷ hoá đơn.");
        }

        return await _invoices.CancelAsync(id)
            ? ServiceResult.Success("Đã huỷ hoá đơn.")
            : ServiceResult.Failure("Không huỷ được hoá đơn đã có thanh toán.");
    }
}
