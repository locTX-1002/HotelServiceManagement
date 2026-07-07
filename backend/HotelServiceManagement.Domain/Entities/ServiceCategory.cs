using System.Collections.Generic;
using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities
{
    public class ServiceCategory : BaseEntity
    {
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<ServiceItem> ServiceItems { get; set; } = new List<ServiceItem>();
    }
}
