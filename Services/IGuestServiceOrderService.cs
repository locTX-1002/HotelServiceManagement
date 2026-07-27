using BusinessObjects.Entities;

namespace Services;

/// <summary>
/// Service rieng cho Guest Portal. Khong tai su dung quyen staff va khong nhan StayId tu UI.
/// </summary>
public interface IGuestServiceOrderService
{
    Task<ServiceResult<Stay>> GetCurrentActiveStayAsync();
    Task<ServiceResult<List<ServiceOrder>>> GetCurrentStayOrdersAsync();
    Task<ServiceResult<ServiceOrder>> CreateAsync(IReadOnlyCollection<ServiceOrderLine> lines);
}
