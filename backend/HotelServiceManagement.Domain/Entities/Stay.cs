using System;
using System.Collections.Generic;
using HotelServiceManagement.Domain.Common;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Domain.Entities
{
    public class Stay : BaseEntity
    {
        public int ReservationId { get; set; }
        public virtual Reservation Reservation { get; set; } = null!;
        public DateTime ActualCheckIn { get; set; }
        public DateTime? ActualCheckOut { get; set; }
        public StayStatus Status { get; set; } = StayStatus.Active;

        // Navigation property for 0..1 relationship with Invoice
        public virtual Invoice? Invoice { get; set; }

        // Navigation property for ServiceOrders
        public virtual ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
    }
}
