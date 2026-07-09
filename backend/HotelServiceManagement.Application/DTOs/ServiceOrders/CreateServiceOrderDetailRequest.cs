namespace HotelServiceManagement.Application.DTOs.ServiceOrders;

public class CreateServiceOrderDetailRequest
{
    public int ServiceItemId { get; set; }
    public int Quantity { get; set; }
}
