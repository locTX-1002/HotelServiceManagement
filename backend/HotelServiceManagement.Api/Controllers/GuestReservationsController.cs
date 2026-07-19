using System.Security.Claims;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Reservations;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    // Khach tu dat phong online - tach rieng khoi ReservationsController (staff-only), dung chung
    // policy "GuestOnly" nhu toan bo cong khach.
    [ApiController]
    [Route("api/guest")]
    [Authorize(Policy = "GuestOnly")]
    public class GuestReservationsController : ControllerBase
    {
        private readonly IGuestReservationService _guestReservationService;

        public GuestReservationsController(IGuestReservationService guestReservationService)
        {
            _guestReservationService = guestReservationService;
        }

        [HttpGet("available-room-types")]
        public async Task<IActionResult> GetAvailableRoomTypes(
            [FromQuery] DateTime checkInDate,
            [FromQuery] DateTime checkOutDate,
            [FromQuery] int? numberOfGuests)
        {
            return ToActionResult(
                await _guestReservationService.GetAvailableRoomTypesAsync(checkInDate, checkOutDate, numberOfGuests));
        }

        [HttpPost("reservations")]
        public async Task<IActionResult> CreateReservation([FromBody] GuestCreateReservationRequest request)
        {
            var guestId = GetCurrentGuestId();
            if (guestId == null)
            {
                return Unauthorized(new AuthMessageResponse { Message = "Invalid guest identity in token." });
            }

            return ToActionResult(await _guestReservationService.CreateAsync(guestId.Value, request));
        }

        private int? GetCurrentGuestId()
        {
            var guestIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(guestIdClaim, out var guestId) ? guestId : null;
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
