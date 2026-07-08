using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Rooms;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return ToActionResult(await _roomService.GetAllAsync());
        }

        [HttpGet("map")]
        public async Task<IActionResult> GetMap()
        {
            return ToActionResult(await _roomService.GetMapAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            return ToActionResult(await _roomService.GetByIdAsync(id));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([FromBody] CreateRoomRequest request)
        {
            var result = await _roomService.CreateAsync(request);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
                : ToActionResult(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomRequest request)
        {
            return ToActionResult(await _roomService.UpdateAsync(id, request));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            return ToActionResult(await _roomService.DeleteAsync(id));
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
