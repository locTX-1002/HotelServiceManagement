using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Housekeeping;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IHousekeepingRequestService
    {
        Task<AuthServiceResult<HousekeepingRequestResponse>> CreateForGuestAsync(int guestId, string? requestType, string? note);
        Task<AuthServiceResult<IReadOnlyList<HousekeepingRequestResponse>>> GetAsync(bool includeCompleted);
        Task<AuthServiceResult<IReadOnlyList<HousekeepingRequestResponse>>> GetForGuestAsync(int guestId);
        Task<AuthServiceResult<HousekeepingRequestResponse>> AcknowledgeAsync(int id, int staffUserId);
        Task<AuthServiceResult<HousekeepingRequestResponse>> CompleteAsync(int id, int staffUserId);
        Task<AuthServiceResult<HousekeepingRequestResponse>> CancelAsync(int id, int staffUserId);
    }
}
