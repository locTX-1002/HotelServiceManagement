using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities;

public class ServiceItem : BaseAuditableEntity
{
    public int ServiceItemId { get; set; }
    public int ServiceCategoryId { get; set; }
    public string ServiceName { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public bool IsAvailable { get; set; } = true;

    public ServiceCategory ServiceCategory { get; set; } = null!;
    public ICollection<ServiceOrderDetail> ServiceOrderDetails { get; set; } = new List<ServiceOrderDetail>();
}
