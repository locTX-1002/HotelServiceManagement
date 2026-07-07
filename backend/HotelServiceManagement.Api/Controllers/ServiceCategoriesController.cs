using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/service-categories")]
    [Authorize]
    public class ServiceCategoriesController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(new[] {
                new { Id = 1, CategoryName = "Restaurant" },
                new { Id = 2, CategoryName = "Laundry" }
            });
        }
    }
}
