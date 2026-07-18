using System.Security.Claims;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    // Chuong thong bao phia nhan vien - dung cho Admin/Manager/Receptionist (dung nhu yeu cau goc:
    // "le tan hoac admin se co 1 cai chuong nhan thong bao"), khong mo cho ServiceStaff vi don phong
    // khac voi dich vu nha hang/giat ui ho phu trach.
    [ApiController]
    [Route("api/housekeeping-requests")]
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public class HousekeepingRequestsController : ControllerBase
    {
        private readonly IHousekeepingRequestService _housekeepingRequestService;

        public HousekeepingRequestsController(IHousekeepingRequestService housekeepingRequestService)
        {
            _housekeepingRequestService = housekeepingRequestService;
        }

        // Tra ve moi yeu cau CHUA hoan tat (Pending + Acknowledged) - du de chuong hien danh sach day
        // du, FE tu tinh badge tu rieng so Pending.
        [HttpGet]
        public async Task<IActionResult> GetActive()
        {
            return ToActionResult(await _housekeepingRequestService.GetActiveAsync());
        }

        [HttpPatch("{id:int}/acknowledge")]
        public async Task<IActionResult> Acknowledge(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new AuthMessageResponse { Message = "Invalid user identity in token." });
            }

            return ToActionResult(await _housekeepingRequestService.AcknowledgeAsync(id, userId.Value));
        }

        [HttpPatch("{id:int}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new AuthMessageResponse { Message = "Invalid user identity in token." });
            }

            return ToActionResult(await _housekeepingRequestService.CompleteAsync(id, userId.Value));
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
