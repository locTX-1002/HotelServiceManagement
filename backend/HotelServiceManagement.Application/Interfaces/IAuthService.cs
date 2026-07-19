using HotelServiceManagement.Application.DTOs.Auth;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
        Task<AuthServiceResult<LoginResponse>> GoogleLoginAsync(string idToken, bool rememberMe);
        Task<AuthServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<AuthServiceResult<AuthMessageResponse>> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<AuthServiceResult<CurrentUserResponse>> GetCurrentUserAsync(int userId);
        Task<AuthServiceResult<AuthMessageResponse>> ForgotPasswordAsync(string email);
        Task<AuthServiceResult<AuthMessageResponse>> ResetPasswordWithTokenAsync(string token, string newPassword);
    }
}
