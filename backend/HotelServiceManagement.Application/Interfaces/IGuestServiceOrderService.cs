using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceItems;
using HotelServiceManagement.Application.DTOs.ServiceOrders;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IGuestServiceOrderService
    {
        Task<AuthServiceResult<IReadOnlyList<ServiceItemResponse>>> GetCatalogAsync();
        Task<AuthServiceResult<ServiceOrderResponse>> CreateOrderAsync(int guestId, GuestCreateServiceOrderRequest request);
    }
}
