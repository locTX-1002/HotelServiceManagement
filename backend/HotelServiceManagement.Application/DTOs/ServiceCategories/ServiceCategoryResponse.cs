namespace HotelServiceManagement.Application.DTOs.ServiceCategories;

public class ServiceCategoryResponse
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
