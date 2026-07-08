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
