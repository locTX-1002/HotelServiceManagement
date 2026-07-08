using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.RoomTypes;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/room-types")]
    [Authorize]
    public class RoomTypesController : ControllerBase
    {
        private readonly IRoomTypeService _roomTypeService;

        public RoomTypesController(IRoomTypeService roomTypeService)
        {
            _roomTypeService = roomTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return ToActionResult(await _roomTypeService.GetAllAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            return ToActionResult(await _roomTypeService.GetByIdAsync(id));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([FromBody] CreateRoomTypeRequest request)
        {
            var result = await _roomTypeService.CreateAsync(request);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
                : ToActionResult(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomTypeRequest request)
        {
            return ToActionResult(await _roomTypeService.UpdateAsync(id, request));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            return ToActionResult(await _roomTypeService.DeleteAsync(id));
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
