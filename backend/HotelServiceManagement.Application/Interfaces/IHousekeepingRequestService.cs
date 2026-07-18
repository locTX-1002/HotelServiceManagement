using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Housekeeping;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IHousekeepingRequestService
    {
        Task<AuthServiceResult<HousekeepingRequestResponse>> CreateForGuestAsync(int guestId, string? note);
        Task<AuthServiceResult<IReadOnlyList<HousekeepingRequestResponse>>> GetActiveAsync();
        Task<AuthServiceResult<HousekeepingRequestResponse>> AcknowledgeAsync(int id, int staffUserId);
        Task<AuthServiceResult<HousekeepingRequestResponse>> CompleteAsync(int id, int staffUserId);
    }
}
