using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Invoices;
using HotelServiceManagement.Application.Interfaces;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
            {
                return NotFound(new { Message = $"Invoice ID {id} not found." });
            }

            return Ok(invoice);
        }

        [HttpGet("stay/{stayId}")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> GetByStayId(int stayId)
        {
            var invoice = await _invoiceService.GetInvoiceByStayIdAsync(stayId);
            if (invoice == null)
            {
                return NotFound(new { Message = $"Invoice for stay ID {stayId} not found." });
            }
            return Ok(invoice);
        }

        [HttpPost("stay/{stayId}")]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Create(int stayId, [FromBody] CreateInvoiceRequest? request)
        {
            return ToActionResult(
                await _invoiceService.CreateInvoiceAsync(stayId, GetCurrentUserId(), request?.PromotionCode));
        }

        [HttpPatch("{id:int}/cancel")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Cancel(int id)
        {
            return ToActionResult(await _invoiceService.CancelAsync(id));
        }

        private int GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("userId")?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token.");
            }

            return userId;
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
