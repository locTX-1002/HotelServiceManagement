using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelServiceManagement.Application.DTOs.Payments;
using HotelServiceManagement.Application.Interfaces;

namespace HotelServiceManagement.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            var result = await _paymentService.ProcessPaymentAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
