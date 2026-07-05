using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities;

public class ServiceOrderDetail : BaseAuditableEntity
{
    public int DetailId { get; set; }
    public int ServiceOrderId { get; set; }
    public int ServiceItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }

    public ServiceOrder ServiceOrder { get; set; } = null!;
    public ServiceItem ServiceItem { get; set; } = null!;
}
