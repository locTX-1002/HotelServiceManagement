using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceOrders;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IServiceOrderService
    {
        Task<AuthServiceResult<IReadOnlyList<ServiceOrderResponse>>> GetAllAsync();
        Task<AuthServiceResult<ServiceOrderResponse>> GetByIdAsync(int id);
        Task<AuthServiceResult<ServiceOrderResponse>> CreateAsync(CreateServiceOrderRequest request, int createdByUserId);
        Task<AuthServiceResult<ServiceOrderResponse>> UpdateStatusAsync(int id, UpdateServiceOrderStatusRequest request);
    }
}
