using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/service-items")]
    [Authorize]
    public class ServiceItemsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(new[] {
                new { Id = 1, ItemName = "Breakfast Set", Price = 15.00, CategoryId = 1 },
                new { Id = 4, ItemName = "Shirt Washing", Price = 5.00, CategoryId = 2 }
            });
        }
    }
}
