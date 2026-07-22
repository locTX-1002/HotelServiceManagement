using BusinessObjects.Entities; using BusinessObjects.Enums;
namespace Services;
public record ServiceOrderLine(int ServiceItemId,int Quantity);
public interface IServiceOrderService { Task<List<ServiceOrder>> GetByStayAsync(int stayId); Task<ServiceResult<ServiceOrder>> CreateAsync(int stayId,IReadOnlyCollection<ServiceOrderLine> lines); Task<ServiceResult<ServiceOrder>> ChangeStatusAsync(int id,ServiceOrderStatus status); }
