using BusinessObjects.Entities; using BusinessObjects.Enums; using DataAccessObjects;
namespace Repositories;
public sealed class ServiceOrderRepository : IServiceOrderRepository
{
    public Task<List<ServiceOrder>> GetByStayAsync(int id)=>ServiceOrderDAO.Instance.GetByStayAsync(id); public Task<bool> IsStayActiveAsync(int id)=>ServiceOrderDAO.Instance.IsStayActiveAsync(id);
    public Task AddAsync(ServiceOrder x)=>ServiceOrderDAO.Instance.AddAsync(x); public Task<ServiceOrder?> ChangeStatusAsync(int id,ServiceOrderStatus s)=>ServiceOrderDAO.Instance.ChangeStatusAsync(id,s);
}
