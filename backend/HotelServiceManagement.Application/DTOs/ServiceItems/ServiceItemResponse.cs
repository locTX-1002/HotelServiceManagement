namespace HotelServiceManagement.Application.DTOs.ServiceItems;

public class ServiceItemResponse
{
    public int Id { get; set; }
    public int ServiceCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsAvailable { get; set; }
}
