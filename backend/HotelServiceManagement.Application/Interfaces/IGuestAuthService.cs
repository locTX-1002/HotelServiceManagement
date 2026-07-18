using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.GuestAuth;
using HotelServiceManagement.Application.DTOs.Reservations;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IGuestAuthService
    {
        Task<AuthServiceResult<GuestAuthResponse>> RegisterAsync(GuestRegisterRequest request);
        Task<AuthServiceResult<GuestAuthResponse>> LoginAsync(GuestLoginRequest request);
        Task<AuthServiceResult<GuestAuthResponse>> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<AuthServiceResult<AuthMessageResponse>> ForgotPasswordAsync(string phoneNumber);
        Task<AuthServiceResult<AuthMessageResponse>> ResetPasswordWithTokenAsync(string token, string newPassword);
        Task<AuthServiceResult<IReadOnlyList<ReservationResponse>>> GetMyReservationsAsync(int guestId);
    }
}
