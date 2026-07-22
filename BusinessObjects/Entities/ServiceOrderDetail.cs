using BusinessObjects.Common;

namespace BusinessObjects.Entities
{
    public class ServiceOrderDetail : BaseEntity
    {
        public int ServiceOrderId { get; set; }
        public virtual ServiceOrder ServiceOrder { get; set; } = null!;
        public int ServiceItemId { get; set; }
        public virtual ServiceItem ServiceItem { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}
