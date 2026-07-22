using BusinessObjects.Entities;
using BusinessObjects.Enums;
namespace Services;

public interface IHousekeepingRequestService
{
    Task<List<HousekeepingRequest>> GetAllAsync();
    Task<ServiceResult<HousekeepingRequest>> CreateAsync(int stayId, HousekeepingRequestType type, string? note);
    Task<ServiceResult<HousekeepingRequest>> ChangeStatusAsync(int id, HousekeepingRequestStatus status);
}
