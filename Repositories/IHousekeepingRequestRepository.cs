using BusinessObjects.Entities;
namespace Repositories;

public interface IHousekeepingRequestRepository
{
    Task<List<HousekeepingRequest>> GetAllAsync();
    Task<HousekeepingRequest?> GetByIdAsync(int id);
    Task<bool> IsStayActiveAsync(int stayId);
    Task SaveAsync(HousekeepingRequest request, bool add);
}
