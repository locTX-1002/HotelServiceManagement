using System.Security.Claims;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.GuestAuth;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    // Cong rieng cho khach (guest portal) - tach hoan toan khoi AuthController cua nhan vien.
    // Token phat ra tu day mang claim token_scope=guest, chi qua duoc policy "GuestOnly" o day,
    // khong bao gio qua duoc DefaultPolicy (token_scope=staff) ma toan bo API van hanh dang dung.
    [ApiController]
    [Route("api/guest")]
    public class GuestAuthController : ControllerBase
    {
        private readonly IGuestAuthService _guestAuthService;

        public GuestAuthController(IGuestAuthService guestAuthService)
        {
            _guestAuthService = guestAuthService;
        }

        [HttpPost("auth/register")]
        public async Task<IActionResult> Register([FromBody] GuestRegisterRequest request)
        {
            return ToActionResult(await _guestAuthService.RegisterAsync(request));
        }

        [HttpPost("auth/login")]
        public async Task<IActionResult> Login([FromBody] GuestLoginRequest request)
        {
            return ToActionResult(await _guestAuthService.LoginAsync(request));
        }

        // Khong [Authorize] - muc dich la lam moi access token DA het han.
        [HttpPost("auth/refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            return ToActionResult(await _guestAuthService.RefreshTokenAsync(request?.RefreshToken ?? string.Empty));
        }

        [HttpPost("auth/logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            await _guestAuthService.LogoutAsync(request?.RefreshToken ?? string.Empty);
            return Ok(new AuthMessageResponse { Message = "Logged out." });
        }

        [HttpPost("auth/reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] GuestResetPasswordRequest request)
        {
            return ToActionResult(await _guestAuthService.ResetPasswordAsync(request));
        }

        [Authorize(Policy = "GuestOnly")]
        [HttpGet("me/reservations")]
        public async Task<IActionResult> GetMyReservations()
        {
            var guestId = GetCurrentGuestId();
            if (guestId == null)
            {
                return Unauthorized(new AuthMessageResponse { Message = "Invalid guest identity in token." });
            }

            return ToActionResult(await _guestAuthService.GetMyReservationsAsync(guestId.Value));
        }

        private int? GetCurrentGuestId()
        {
            var guestIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(guestIdClaim, out var guestId) ? guestId : null;
        }

        private IActionResult ToActionResult<T>(AuthServiceResult<T> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            var body = new AuthMessageResponse { Message = result.Message };
            return result.StatusCode switch
            {
                401 => Unauthorized(body),
                404 => NotFound(body),
                409 => Conflict(body),
                _ => BadRequest(body)
            };
        }
    }
}
