using HotelServiceManagement.Application.DTOs.Auth;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
        Task<AuthServiceResult<AuthMessageResponse>> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<AuthServiceResult<CurrentUserResponse>> GetCurrentUserAsync(int userId);
    }
}
