using BusinessObjects.Entities;
namespace Services;

public interface IInvoiceService { Task<Invoice?> GetByIdAsync(int id); Task<Invoice?> GetByStayAsync(int stayId); Task<ServiceResult<Invoice>> PrepareAsync(int stayId, string? promotionCode = null, DateTime? asOf = null); Task<ServiceResult> CancelAsync(int id); }
