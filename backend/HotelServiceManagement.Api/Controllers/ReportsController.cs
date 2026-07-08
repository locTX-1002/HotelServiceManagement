using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Admin,Manager")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            return ToActionResult(await _reportService.GetDashboardAsync());
        }

        [HttpGet("occupancy")]
        public async Task<IActionResult> GetOccupancy()
        {
            return ToActionResult(await _reportService.GetOccupancyAsync());
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            return ToActionResult(await _reportService.GetRevenueAsync(fromDate, toDate));
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
