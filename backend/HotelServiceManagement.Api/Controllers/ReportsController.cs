using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Admin,Manager")]
    public class ReportsController : ControllerBase
    {
        [HttpGet("revenue")]
        public IActionResult GetRevenueReport()
        {
            return Ok(new { TotalRevenue = 15000.00, Period = "Monthly", Message = "Revenue report placeholder" });
        }
    }
}
