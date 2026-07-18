namespace HotelServiceManagement.Application.DTOs.Surcharges;

public class SurchargeItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
