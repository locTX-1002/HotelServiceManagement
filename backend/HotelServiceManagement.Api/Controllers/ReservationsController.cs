using System.Security.Claims;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Reservations;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/reservations")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return ToActionResult(await _reservationService.GetAllAsync());
        }

        [HttpGet("available-rooms")]
        public async Task<IActionResult> GetAvailableRooms(
            [FromQuery] DateTime checkInDate,
            [FromQuery] DateTime checkOutDate,
            [FromQuery] int? roomTypeId,
            [FromQuery] int? capacity)
        {
            return ToActionResult(
                await _reservationService.GetAvailableRoomsAsync(
                    checkInDate,
                    checkOutDate,
                    roomTypeId,
                    capacity));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            return ToActionResult(await _reservationService.GetByIdAsync(id));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new AuthMessageResponse
                {
                    Message = "Invalid or missing user id in access token."
                });
            }

            var result = await _reservationService.CreateAsync(
                request,
                currentUserId.Value);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
                : ToActionResult(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateReservationRequest request)
        {
            return ToActionResult(
                await _reservationService.UpdateAsync(id, request));
        }

        [HttpPatch("{id:int}/cancel")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Cancel(int id)
        {
            return ToActionResult(await _reservationService.CancelAsync(id));
        }

        [HttpPatch("{id:int}/no-show")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> NoShow(int id)
        {
            return ToActionResult(await _reservationService.NoShowAsync(id));
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("userId");

            return int.TryParse(userIdValue, out var userId) && userId > 0
                ? userId
                : null;
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