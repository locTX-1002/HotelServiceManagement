using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/guests")]
    [Authorize]
    public class GuestsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(new[] {
                new { Id = 1, FullName = "Default Guest", Email = "guest@example.com", PhoneNumber = "0987654321", IdentityNumber = "123456789" }
            });
        }
    }
}
