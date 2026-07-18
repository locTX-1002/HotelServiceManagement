namespace HotelServiceManagement.Application.DTOs.Surcharges;

public class SurchargeLineResponse
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}
