namespace HotelServiceManagement.Application.DTOs.ServiceOrders;

public class CreateServiceOrderRequest
{
    public int StayId { get; set; }
    public List<CreateServiceOrderDetailRequest> Details { get; set; } = new();
}
