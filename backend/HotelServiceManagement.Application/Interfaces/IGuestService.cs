using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Guests;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IGuestService
    {
        Task<AuthServiceResult<IReadOnlyList<GuestResponse>>> GetAllAsync(string? keyword = null);
        Task<AuthServiceResult<GuestResponse>> GetByIdAsync(int id);
        Task<AuthServiceResult<GuestResponse>> CreateAsync(CreateGuestRequest request);
        Task<AuthServiceResult<GuestResponse>> UpdateAsync(int id, UpdateGuestRequest request);
    }
}
