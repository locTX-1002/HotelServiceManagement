using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Application.DTOs.ServiceOrders;

public class ServiceOrderResponse
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public DateTime OrderDate { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public IReadOnlyList<ServiceOrderDetailResponse> Details { get; set; } = Array.Empty<ServiceOrderDetailResponse>();
}
