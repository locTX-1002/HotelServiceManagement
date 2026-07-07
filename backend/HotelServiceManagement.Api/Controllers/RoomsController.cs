using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(new[] {
                new { Id = 1, RoomNumber = "101", RoomTypeId = 1, Status = "Available" },
                new { Id = 2, RoomNumber = "102", RoomTypeId = 1, Status = "Available" }
            });
        }
    }
}
