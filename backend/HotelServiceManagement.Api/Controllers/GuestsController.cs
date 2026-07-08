using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Guests;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/guests")]
    [Authorize]
    public class GuestsController : ControllerBase
    {
        private readonly IGuestService _guestService;

        public GuestsController(IGuestService guestService)
        {
            _guestService = guestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? keyword)
        {
            return ToActionResult(await _guestService.GetAllAsync(keyword));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            return ToActionResult(await _guestService.GetByIdAsync(id));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> Create([FromBody] CreateGuestRequest request)
        {
            var result = await _guestService.CreateAsync(request);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
                : ToActionResult(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateGuestRequest request)
        {
            return ToActionResult(await _guestService.UpdateAsync(id, request));
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
