using System.Security.Claims;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceOrders;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/service-orders")]
    [Authorize]
    public class ServiceOrdersController : ControllerBase
    {
        private readonly IServiceOrderService _serviceOrderService;

        public ServiceOrdersController(IServiceOrderService serviceOrderService)
        {
            _serviceOrderService = serviceOrderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return ToActionResult(await _serviceOrderService.GetAllAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            return ToActionResult(await _serviceOrderService.GetByIdAsync(id));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Receptionist,ServiceStaff")]
        public async Task<IActionResult> Create([FromBody] CreateServiceOrderRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized(new AuthMessageResponse
                {
                    Message = "Invalid or missing user id in access token."
                });
            }

            var result = await _serviceOrderService.CreateAsync(
                request,
                currentUserId.Value);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
                : ToActionResult(result);
        }

        [HttpPatch("{id:int}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,ServiceStaff")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateServiceOrderStatusRequest request)
        {
            return ToActionResult(
                await _serviceOrderService.UpdateStatusAsync(id, request));
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