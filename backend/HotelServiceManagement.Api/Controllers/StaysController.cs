using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelServiceManagement.Application.DTOs.Stays;
using HotelServiceManagement.Application.Interfaces;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/stays")]
    [Authorize]
    public class StaysController : ControllerBase
    {
        private readonly IStayService _stayService;

        public StaysController(IStayService stayService)
        {
            _stayService = stayService;
        }

        // ServiceStaff cần xem lượt ở đang Active để chọn đúng khách khi tạo đơn dịch vụ (/service-orders).
        // Manager không có trang Check-in/Check-out lẫn Gọi dịch vụ trên FE nên bỏ khỏi endpoint ghi/đọc vận hành này.
        [HttpGet("active")]
        [Authorize(Roles = "Admin,Receptionist,ServiceStaff")]
        public async Task<IActionResult> GetActive()
        {
            return Ok(await _stayService.GetActiveAsync());
        }

        [HttpPost("check-in")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
        {
            var result = await _stayService.CheckInAsync(request, GetCurrentUserId());
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{id}/check-out")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> CheckOut(int id)
        {
            var result = await _stayService.CheckOutAsync(id, GetCurrentUserId());
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("userId")?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token.");
            }

            return userId;
        }
    }
}
