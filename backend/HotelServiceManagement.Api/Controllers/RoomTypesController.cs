using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/room-types")]
    [Authorize]
    public class RoomTypesController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(new[] {
                new { Id = 1, TypeName = "Standard", PricePerNight = 100.00 },
                new { Id = 2, TypeName = "Deluxe", PricePerNight = 180.00 },
                new { Id = 3, TypeName = "Suite", PricePerNight = 300.00 },
                new { Id = 4, TypeName = "Family Room", PricePerNight = 250.00 }
            });
        }
    }
}
