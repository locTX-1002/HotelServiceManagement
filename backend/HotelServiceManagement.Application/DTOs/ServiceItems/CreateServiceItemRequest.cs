namespace HotelServiceManagement.Application.DTOs.ServiceItems;

public class CreateServiceItemRequest
{
    public int ServiceCategoryId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsAvailable { get; set; } = true;
}
