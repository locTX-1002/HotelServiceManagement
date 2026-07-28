using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace FUHotelManagementWPF.ViewModels.GuestPortal
{
    /// <summary>
    /// Một hóa đơn của khách đang đăng nhập, đã gộp sẵn thông tin phòng và ngày ở.
    /// </summary>
    public class MyInvoiceRow
    {
        public Invoice Invoice { get; }

        public string Title { get; }

        public InvoiceStatus Status => Invoice.Status;

        public bool IsUnpaid => Invoice.Status == InvoiceStatus.Unpaid;
        public bool IsPartiallyPaid => Invoice.Status == InvoiceStatus.PartiallyPaid;
        public bool IsPaid => Invoice.Status == InvoiceStatus.Paid;
        public bool IsCancelled => Invoice.Status == InvoiceStatus.Cancelled;

        public string StatusText => Invoice.Status switch
        {
            InvoiceStatus.Unpaid => "Chưa thanh toán",
            InvoiceStatus.PartiallyPaid => "Trả một phần",
            InvoiceStatus.Paid => "Đã thanh toán",
            InvoiceStatus.Cancelled => "Đã huỷ",
            _ => string.Empty,
        };

        public bool HasVipDiscount => Invoice.IsVipDiscountApplied;

        public string VipDiscountText => "Ưu đãi khách VIP - giảm 10%";

        public string OtherPromotionCode => Invoice.PromotionCode ?? string.Empty;

        public bool HasOtherPromotion => OtherPromotionCode.Length > 0;

        public string OtherPromotionText => $"Mã khuyến mại: {OtherPromotionCode}";

        public bool HasManualDiscount => Invoice.ManualDiscountAmount > 0;

        public string ManualDiscountText
            => $"Giảm giá được quản lý phê duyệt: {Invoice.ManualDiscountAmount:N0} đ";

        public MyInvoiceRow(Reservation reservation, Invoice invoice)
        {
            Invoice = invoice;

            var roomNumber = reservation.Room?.RoomNumber ?? "—";
            var checkIn = reservation.Stay?.ActualCheckIn ?? reservation.CheckInDate;
            var checkOut = reservation.Stay?.ActualCheckOut ?? reservation.CheckOutDate;
            Title = $"Phòng {roomNumber} - {checkIn:dd/MM/yyyy} đến {checkOut:dd/MM/yyyy}";
        }
    }
}
