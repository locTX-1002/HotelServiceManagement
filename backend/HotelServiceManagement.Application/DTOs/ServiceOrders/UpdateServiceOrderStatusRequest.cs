using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Application.DTOs.ServiceOrders;

public class UpdateServiceOrderStatusRequest
{
    public ServiceOrderStatus Status { get; set; }
}
