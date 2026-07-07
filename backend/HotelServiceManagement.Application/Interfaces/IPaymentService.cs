using System.Threading.Tasks;
using HotelServiceManagement.Application.DTOs.Payments;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request);
    }
}
