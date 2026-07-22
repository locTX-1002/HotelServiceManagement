using BusinessObjects.Common;

namespace BusinessObjects.Entities
{
    public class ServiceItem : BaseEntity
    {
        public int ServiceCategoryId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public bool IsAvailable { get; set; } = true;

        public virtual ServiceCategory ServiceCategory { get; set; } = null!;
    }
}
