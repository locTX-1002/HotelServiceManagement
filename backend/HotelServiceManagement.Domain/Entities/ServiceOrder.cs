using System;
using System.Collections.Generic;
using HotelServiceManagement.Domain.Common;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Domain.Entities
{
    public class ServiceOrder : BaseEntity
    {
        public int StayId { get; set; }
        public virtual Stay Stay { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public ServiceOrderStatus Status { get; set; } = ServiceOrderStatus.Pending;
        public decimal TotalAmount { get; set; }

        // Navigation property
        public virtual ICollection<ServiceOrderDetail> OrderDetails { get; set; } = new List<ServiceOrderDetail>();
    }
}
