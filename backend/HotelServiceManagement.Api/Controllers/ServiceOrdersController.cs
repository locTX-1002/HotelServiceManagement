using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/service-orders")]
    [Authorize]
    public class ServiceOrdersController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(new[] {
                new { Id = 1, StayId = 1, OrderDate = System.DateTime.UtcNow, Status = "Pending" }
            });
        }
    }
}
