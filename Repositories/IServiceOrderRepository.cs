using BusinessObjects.Entities;
using BusinessObjects.Enums;
namespace Repositories;

public interface IServiceOrderRepository { Task<List<ServiceOrder>> GetByStayAsync(int stayId); Task<ServiceOrder?> GetByIdAsync(int id); Task<bool> IsStayActiveAsync(int stayId); Task AddAsync(ServiceOrder x); Task<ServiceOrder?> ChangeStatusAsync(int id, ServiceOrderStatus status); }
