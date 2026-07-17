using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Surcharges;

namespace HotelServiceManagement.Application.Interfaces;

public interface ISurchargeItemService
{
    Task<AuthServiceResult<IReadOnlyList<SurchargeItemResponse>>> GetAllAsync();
    Task<AuthServiceResult<SurchargeItemResponse>> GetByIdAsync(int id);
    Task<AuthServiceResult<SurchargeItemResponse>> CreateAsync(SurchargeItemRequest request);
    Task<AuthServiceResult<SurchargeItemResponse>> UpdateAsync(int id, SurchargeItemRequest request);
}
