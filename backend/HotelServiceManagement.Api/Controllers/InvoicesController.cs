using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> Create(int stayId)
        {
            var invoice = await _invoiceService.CreateInvoiceAsync(stayId);
            if (invoice == null)
            {
                return BadRequest(new { Message = "Failed to create invoice." });
            }
            return Ok(invoice);
        }
    }
}
