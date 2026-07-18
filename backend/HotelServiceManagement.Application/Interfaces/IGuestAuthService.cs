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
        Task<AuthServiceResult<AuthMessageResponse>> ResetPasswordAsync(GuestResetPasswordRequest request);
        Task<AuthServiceResult<IReadOnlyList<ReservationResponse>>> GetMyReservationsAsync(int guestId);
    }
}
