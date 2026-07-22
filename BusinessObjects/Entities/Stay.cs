using System;
using System.Collections.Generic;
using BusinessObjects.Common;
using BusinessObjects.Enums;

namespace BusinessObjects.Entities
{
    public class Stay : BaseEntity
    {
        public int ReservationId { get; set; }
        public virtual Reservation Reservation { get; set; } = null!;
        public DateTime ActualCheckIn { get; set; }
        public DateTime? ActualCheckOut { get; set; }
        public StayStatus Status { get; set; } = StayStatus.Active;
        public int? CheckedInByUserId { get; set; }
        public virtual User? CheckedInByUser { get; set; }
        public int? CheckedOutByUserId { get; set; }
        public virtual User? CheckedOutByUser { get; set; }

        // Navigation property for 0..1 relationship with Invoice
        public virtual Invoice? Invoice { get; set; }

        // Navigation property for ServiceOrders
        public virtual ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
        public virtual ICollection<Surcharge> Surcharges { get; set; } = new List<Surcharge>();
    }
}
