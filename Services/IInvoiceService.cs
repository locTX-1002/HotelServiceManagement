using BusinessObjects.Entities;
namespace Services;

public interface IInvoiceService
{
    Task<Invoice?> GetByIdAsync(int id);
    Task<Invoice?> GetByStayAsync(int stayId);

    /// <summary>
    /// manualDiscount = null: giữ mức giảm tay đã được duyệt trên hóa đơn hiện có.
    /// manualDiscount có giá trị: ghi đè mức giảm tay (chỉ luồng có quyền duyệt).
    /// </summary>
    Task<ServiceResult<Invoice>> PrepareAsync(
        int stayId,
        string? promotionCode = null,
        DateTime? asOf = null,
        decimal? manualDiscount = null);

    Task<ServiceResult<Invoice>> ApplyApprovedDiscountAsync(int stayId, decimal manualDiscount);
    Task<ServiceResult> CancelAsync(int id);
}
