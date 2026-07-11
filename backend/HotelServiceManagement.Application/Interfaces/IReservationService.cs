using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Reservations;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IReservationService
    {
        Task<AuthServiceResult<IReadOnlyList<ReservationResponse>>> GetAllAsync();
        Task<AuthServiceResult<ReservationResponse>> GetByIdAsync(int id);

        // createdByUserId is read from the authenticated JWT by the API controller.
        Task<AuthServiceResult<ReservationResponse>> CreateAsync(
            CreateReservationRequest request,
            int createdByUserId);

        Task<AuthServiceResult<ReservationResponse>> UpdateAsync(
            int id,
            UpdateReservationRequest request);

        Task<AuthServiceResult<AuthMessageResponse>> CancelAsync(int id);

        Task<AuthServiceResult<IReadOnlyList<AvailableRoomResponse>>> GetAvailableRoomsAsync(
            DateTime checkInDate,
            DateTime checkOutDate,
            int? roomTypeId,
            int? capacity);
    }
}