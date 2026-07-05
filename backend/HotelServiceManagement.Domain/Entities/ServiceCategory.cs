using HotelServiceManagement.Domain.Common;

namespace HotelServiceManagement.Domain.Entities;

public class ServiceCategory : BaseAuditableEntity
{
    public int ServiceCategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public ICollection<ServiceItem> ServiceItems { get; set; } = new List<ServiceItem>();
}
