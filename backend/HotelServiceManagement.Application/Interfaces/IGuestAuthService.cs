using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.GuestAuth;
using HotelServiceManagement.Application.DTOs.Reservations;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IGuestAuthService
    {
        Task<AuthServiceResult<GuestAuthResponse>> RegisterAsync(GuestRegisterRequest request);
        Task<AuthServiceResult<GuestAuthResponse>> LoginAsync(GuestLoginRequest request);
        Task<AuthServiceResult<GuestAuthResponse>> GoogleLoginAsync(string idToken, string? phoneNumber, string? fullName, string? password);
        Task<AuthServiceResult<GuestAuthResponse>> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<AuthServiceResult<AuthMessageResponse>> ForgotPasswordAsync(string email);
        Task<AuthServiceResult<AuthMessageResponse>> ResetPasswordWithTokenAsync(string token, string newPassword);
        Task<AuthServiceResult<IReadOnlyList<ReservationResponse>>> GetMyReservationsAsync(int guestId);
        Task<AuthServiceResult<GuestProfileResponse>> GetMyProfileAsync(int guestId);
        Task<AuthServiceResult<GuestProfileResponse>> UpdateMyProfileAsync(int guestId, UpdateGuestProfileRequest request);
        Task<AuthServiceResult<AuthMessageResponse>> ChangeMyPasswordAsync(int guestId, GuestChangePasswordRequest request);
    }
}
