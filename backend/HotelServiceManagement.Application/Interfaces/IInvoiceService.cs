using System.Threading.Tasks;
using HotelServiceManagement.Application.DTOs.Invoices;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceResponse?> GetByIdAsync(int id);
        Task<InvoiceResponse?> GetInvoiceByStayIdAsync(int stayId);
        Task<InvoiceResponse?> CreateInvoiceAsync(int stayId, int createdByUserId);
    }
}
