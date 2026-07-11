using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceOrders;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IServiceOrderService
    {
        Task<AuthServiceResult<IReadOnlyList<ServiceOrderResponse>>> GetAllAsync();
        Task<AuthServiceResult<ServiceOrderResponse>> GetByIdAsync(int id);

        // createdByUserId is read from the authenticated JWT by the API controller.
        Task<AuthServiceResult<ServiceOrderResponse>> CreateAsync(
            CreateServiceOrderRequest request,
            int createdByUserId);

        Task<AuthServiceResult<ServiceOrderResponse>> UpdateStatusAsync(
            int id,
            UpdateServiceOrderStatusRequest request);
    }
}