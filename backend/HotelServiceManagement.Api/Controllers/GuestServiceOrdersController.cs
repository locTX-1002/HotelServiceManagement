using System.Security.Claims;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceOrders;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    // Khach tu xem catalog + dat dich vu trong luc dang luu tru - tach rieng khoi
    // ServiceItemsController/ServiceOrdersController (staff-only), dung chung policy "GuestOnly".
    [ApiController]
    [Route("api/guest")]
    [Authorize(Policy = "GuestOnly")]
    public class GuestServiceOrdersController : ControllerBase
    {
        private readonly IGuestServiceOrderService _guestServiceOrderService;

        public GuestServiceOrdersController(IGuestServiceOrderService guestServiceOrderService)
        {
            _guestServiceOrderService = guestServiceOrderService;
        }

        [HttpGet("service-items")]
        public async Task<IActionResult> GetCatalog()
        {
            return ToActionResult(await _guestServiceOrderService.GetCatalogAsync());
        }

        [HttpPost("me/service-orders")]
        public async Task<IActionResult> CreateOrder([FromBody] GuestCreateServiceOrderRequest request)
        {
            var guestId = GetCurrentGuestId();
            if (guestId == null)
            {
                return Unauthorized(new AuthMessageResponse { Message = "Invalid guest identity in token." });
            }

            return ToActionResult(await _guestServiceOrderService.CreateOrderAsync(guestId.Value, request));
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
