using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceItems;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/service-items")]
    [Authorize]
    public class ServiceItemsController : ControllerBase
    {
        private readonly IServiceItemService _serviceItemService;

        public ServiceItemsController(IServiceItemService serviceItemService)
        {
            _serviceItemService = serviceItemService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return ToActionResult(await _serviceItemService.GetAllAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            return ToActionResult(await _serviceItemService.GetByIdAsync(id));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,ServiceStaff")]
        public async Task<IActionResult> Create([FromBody] CreateServiceItemRequest request)
        {
            var result = await _serviceItemService.CreateAsync(request);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
                : ToActionResult(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager,ServiceStaff")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceItemRequest request)
        {
            return ToActionResult(await _serviceItemService.UpdateAsync(id, request));
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
