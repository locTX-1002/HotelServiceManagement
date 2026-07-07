using System.Threading.Tasks;
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

        [HttpGet("active")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> GetActive()
        {
            return Ok(await _stayService.GetActiveAsync());
        }

        [HttpPost("check-in")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
        {
            var result = await _stayService.CheckInAsync(request);
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
            var result = await _stayService.CheckOutAsync(id);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
