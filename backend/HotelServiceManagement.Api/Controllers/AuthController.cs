using System.Security.Claims;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            return ToActionResult(await _authService.LoginAsync(request));
        }

        // Khong [Authorize] - muc dich chinh la lam moi access token DA het han, doi hoi Bearer con
        // hop le se tu mau thuan.
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            return ToActionResult(await _authService.RefreshTokenAsync(request?.RefreshToken ?? string.Empty));
        }

        // Luon tra 200 du token co hop le hay khong - client se xoa phien local bat ke ket qua.
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            await _authService.LogoutAsync(request?.RefreshToken ?? string.Empty);
            return Ok(new AuthMessageResponse { Message = "Logged out." });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            return ToActionResult(await _authService.GoogleLoginAsync(request?.IdToken ?? string.Empty, request?.RememberMe ?? false));
        }

        // Khong [Authorize] - nguoi dang quen mat khau khong the co Bearer hop le.
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            return ToActionResult(await _authService.ForgotPasswordAsync(request?.Email ?? string.Empty));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithTokenRequest request)
        {
            return ToActionResult(await _authService.ResetPasswordWithTokenAsync(
                request?.Token ?? string.Empty, request?.NewPassword ?? string.Empty));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new AuthMessageResponse { Message = "Invalid user identity in token." });
            }

            return ToActionResult(await _authService.GetCurrentUserAsync(userId.Value));
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new AuthMessageResponse { Message = "Invalid user identity in token." });
            }

            return ToActionResult(await _authService.ChangePasswordAsync(userId.Value, request));
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
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
