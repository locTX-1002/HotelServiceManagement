using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceCategories;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IServiceCategoryService
    {
        Task<AuthServiceResult<IReadOnlyList<ServiceCategoryResponse>>> GetAllAsync();
    }
}
