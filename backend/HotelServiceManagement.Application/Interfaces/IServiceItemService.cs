using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceItems;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IServiceItemService
    {
        Task<AuthServiceResult<IReadOnlyList<ServiceItemResponse>>> GetAllAsync();
        Task<AuthServiceResult<ServiceItemResponse>> GetByIdAsync(int id);
        Task<AuthServiceResult<ServiceItemResponse>> CreateAsync(CreateServiceItemRequest request);
        Task<AuthServiceResult<ServiceItemResponse>> UpdateAsync(int id, UpdateServiceItemRequest request);
    }
}
