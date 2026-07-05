using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.Interfaces;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Message = "Email and Password are required." });
            }

            var response = await _userService.LoginAsync(request);
            if (response == null)
            {
                return Unauthorized(new { Message = "Invalid email or password, or user is inactive." });
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { Message = "Invalid user identity in token." });
            }

            var response = await _userService.GetCurrentUserAsync(userId);
            if (response == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            return Ok(response);
        }
    }
}
