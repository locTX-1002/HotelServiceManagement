using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Reservations;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IGuestReservationService
    {
        Task<AuthServiceResult<IReadOnlyList<AvailableRoomTypeResponse>>> GetAvailableRoomTypesAsync(
            DateTime checkInDate,
            DateTime checkOutDate,
            int? numberOfGuests);

        Task<AuthServiceResult<ReservationResponse>> CreateAsync(int guestId, GuestCreateReservationRequest request);
    }
}
