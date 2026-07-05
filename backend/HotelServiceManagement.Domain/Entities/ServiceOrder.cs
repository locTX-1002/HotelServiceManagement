using HotelServiceManagement.Domain.Common;
using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Domain.Entities;

public class ServiceOrder : BaseAuditableEntity
{
    public int ServiceOrderId { get; set; }
    public int StayId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public ServiceOrderStatus Status { get; set; } = ServiceOrderStatus.Pending;
    public decimal TotalAmount { get; set; }

    public Stay Stay { get; set; } = null!;
    public ICollection<ServiceOrderDetail> Details { get; set; } = new List<ServiceOrderDetail>();
}
