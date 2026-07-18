using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Surcharges;
using HotelServiceManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelServiceManagement.Api.Controllers;

[ApiController]
[Route("api/surcharge-items")]
[Authorize]
public class SurchargeItemsController : ControllerBase
{
    private readonly ISurchargeItemService _service;

    public SurchargeItemsController(ISurchargeItemService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => ToActionResult(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) => ToActionResult(await _service.GetByIdAsync(id));

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] SurchargeItemRequest request)
    {
        var result = await _service.CreateAsync(request);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] SurchargeItemRequest request) =>
        ToActionResult(await _service.UpdateAsync(id, request));

    private IActionResult ToActionResult<T>(AuthServiceResult<T> result)
    {
        if (result.IsSuccess) return Ok(result.Data);
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
