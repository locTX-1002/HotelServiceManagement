using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Promotions;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IPromotionService
    {
        Task<AuthServiceResult<IReadOnlyList<PromotionResponse>>> GetAllAsync();
        Task<AuthServiceResult<PromotionResponse>> GetByIdAsync(int id);
        Task<AuthServiceResult<PromotionResponse>> CreateAsync(CreatePromotionRequest request);
        Task<AuthServiceResult<PromotionResponse>> UpdateAsync(int id, UpdatePromotionRequest request);
    }
}
