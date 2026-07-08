namespace HotelServiceManagement.Application.DTOs.ServiceOrders;

public class ServiceOrderDetailResponse
{
    public int Id { get; set; }
    public int ServiceItemId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}
