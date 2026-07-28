using System;
using System.Collections.Generic;
using BusinessObjects.Common;
using BusinessObjects.Enums;

namespace BusinessObjects.Entities
{
    public class Invoice : BaseEntity
    {
        public int StayId { get; set; }
        public DateTime InvoiceDate { get; set; }

        public decimal RoomCharge { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal SurchargeAmount { get; set; }

        /// <summary>
        /// Tổng số tiền được giảm sau khi cộng mã khuyến mãi, ưu đãi VIP và giảm tay.
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Khóa ngoại tới chương trình khuyến mãi thật. Giảm tay và VIP không dùng trường này.
        /// </summary>
        public int? PromotionId { get; set; }

        /// <summary>
        /// Snapshot mã khuyến mãi tại lúc áp dụng để hóa đơn vẫn đọc được lịch sử.
        /// Chỉ chứa mã thật trong bảng Promotions; không chứa VIP10/TUNHAP.
        /// </summary>
        public string? PromotionCode { get; set; }

        /// <summary>Số tiền giảm thủ công đã được quản lý phê duyệt và thực tế áp dụng.</summary>
        public decimal ManualDiscountAmount { get; set; }

        /// <summary>Đánh dấu hóa đơn đã áp dụng ưu đãi VIP 10% ở lần tính gần nhất.</summary>
        public bool IsVipDiscountApplied { get; set; }

        public decimal TotalAmount { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
        public int? CreatedByUserId { get; set; }
        public virtual User? CreatedByUser { get; set; }

        public virtual Stay Stay { get; set; } = null!;
        public virtual Promotion? Promotion { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
