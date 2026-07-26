using System;
using System.Linq;
using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace FUHotelManagementWPF.ViewModels.GuestPortal
{
    /// <summary>
    /// Mot hoa don cua khach dang dang nhap, da gop san thong tin phong + ngay o
    /// tu don dat phong. Gop o day thay vi doc invoice.Stay.Reservation vi ban ghi
    /// hoa don lay tu GetByStayAsync khong chac Include den tan Room.
    /// </summary>
    public class MyInvoiceRow
    {
        /// <summary>Nhan ma khach san ghi vao PromotionCode khi giam gia cho khach VIP.</summary>
        private const string VipCode = "VIP10";

        public Invoice Invoice { get; }

        public string Title { get; }

        public InvoiceStatus Status => Invoice.Status;

        // Moi trang thai mot co rieng: XAML khong doi duoc Style tu DataTrigger,
        // nen View dat 4 badge chong len nhau va chi bat dung mot cai.
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

        /// <summary>Hoa don co phan giam 10% danh cho khach VIP.</summary>
        public bool HasVipDiscount { get; }

        public string VipDiscountText => "Ưu đãi khách VIP - giảm 10%";

        /// <summary>Ma khuyen mai thuong (da bo phan nhan VIP), rong neu khong co.</summary>
        public string OtherPromotionCode { get; }

        public bool HasOtherPromotion => OtherPromotionCode.Length > 0;

        public string OtherPromotionText => $"Mã khuyến mại: {OtherPromotionCode}";

        public MyInvoiceRow(Reservation reservation, Invoice invoice)
        {
            Invoice = invoice;

            var roomNumber = reservation.Room?.RoomNumber ?? "—";
            // Ngay nhan/tra thuc te nam o ky luu tru; chua tra phong thi lay ngay du kien
            // trong don dat de khach van doc duoc khoang thoi gian.
            var checkIn = reservation.Stay?.ActualCheckIn ?? reservation.CheckInDate;
            var checkOut = reservation.Stay?.ActualCheckOut ?? reservation.CheckOutDate;
            Title = $"Phòng {roomNumber} - {checkIn:dd/MM/yyyy} đến {checkOut:dd/MM/yyyy}";

            // PromotionCode co the la "SUMMER+VIP10" khi vua nhap ma vua duoc uu dai VIP,
            // nen tach tung phan ra thay vi so sanh ca chuoi.
            var parts = (invoice.PromotionCode ?? string.Empty)
                .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            HasVipDiscount = parts.Any(p => p.Equals(VipCode, StringComparison.OrdinalIgnoreCase));
            OtherPromotionCode = string.Join(", ",
                parts.Where(p => !p.Equals(VipCode, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
