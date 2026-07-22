using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Repositories
{
    public interface IReservationRepository
    {
        Task<List<Reservation>> GetAllAsync();
        Task<Reservation?> GetByIdAsync(int id);
        Task<bool> HasStayAsync(int reservationId);
        Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int? roomTypeId, int? capacity);
        Task<bool> HasOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeId);
        Task<Room?> GetReservableRoomAsync(int roomId);
        Task<Guest?> GetGuestAsync(int guestId);
        Task<bool> BookingCodeExistsAsync(string code);
        Task<int> CreateAsync(Reservation reservation);
        Task UpdateAsync(int id, int guestId, int roomId, int numberOfGuests,
            DateTime checkIn, DateTime checkOut, ReservationStatus status, string? specialRequests);
        Task SetStatusAsync(int id, ReservationStatus status);
    }
}
