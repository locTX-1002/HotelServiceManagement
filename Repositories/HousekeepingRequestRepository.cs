using BusinessObjects.Entities;
using DataAccessObjects;
namespace Repositories;

public sealed class HousekeepingRequestRepository : IHousekeepingRequestRepository
{
    public Task<List<HousekeepingRequest>> GetAllAsync() => HousekeepingRequestDAO.Instance.GetAllAsync();
    public Task<HousekeepingRequest?> GetByIdAsync(int id) => HousekeepingRequestDAO.Instance.GetByIdAsync(id);
    public Task<bool> IsStayActiveAsync(int id) => HousekeepingRequestDAO.Instance.IsStayActiveAsync(id);
    public Task SaveAsync(HousekeepingRequest x, bool add) => HousekeepingRequestDAO.Instance.SaveAsync(x, add);
}
