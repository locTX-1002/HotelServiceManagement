using System.Collections.Generic;
using BusinessObjects.Common;

namespace BusinessObjects.Entities
{
    public class ServiceCategory : BaseEntity
    {
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<ServiceItem> ServiceItems { get; set; } = new List<ServiceItem>();
    }
}
