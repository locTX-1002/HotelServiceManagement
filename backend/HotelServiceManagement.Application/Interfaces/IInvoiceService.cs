using System.Threading.Tasks;
using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Invoices;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceResponse?> GetByIdAsync(int id);
        Task<InvoiceResponse?> GetInvoiceByStayIdAsync(int stayId);

        Task<AuthServiceResult<InvoiceResponse>> CreateInvoiceAsync(
            int stayId,
            int createdByUserId,
            string? promotionCode = null);
        Task<AuthServiceResult<InvoiceResponse>> CancelAsync(int id);
    }
}
