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
        public decimal DiscountAmount { get; set; }
        public string? PromotionCode { get; set; }
        public decimal TotalAmount { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
        public int? CreatedByUserId { get; set; }
        public virtual User? CreatedByUser { get; set; }

        public virtual Stay Stay { get; set; } = null!;
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
