using BusinessObjects.Entities;
namespace Services;

public interface IInvoiceService { Task<Invoice?> GetByIdAsync(int id); Task<Invoice?> GetByStayAsync(int stayId); Task<ServiceResult<Invoice>> PrepareAsync(int stayId, string? promotionCode = null, DateTime? asOf = null, decimal manualDiscount = 0); Task<ServiceResult<Invoice>> ApplyApprovedDiscountAsync(int stayId, decimal manualDiscount); Task<ServiceResult> CancelAsync(int id); }
