using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;

namespace Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        public Task<List<Reservation>> GetAllAsync() => ReservationDAO.Instance.GetAllAsync();
        public Task<Reservation?> GetByIdAsync(int id) => ReservationDAO.Instance.GetByIdAsync(id);
        public Task<bool> HasStayAsync(int reservationId) => ReservationDAO.Instance.HasStayAsync(reservationId);

        public Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int? roomTypeId, int? capacity)
            => ReservationDAO.Instance.GetAvailableRoomsAsync(checkIn, checkOut, roomTypeId, capacity);

        public Task<bool> HasOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeId)
            => ReservationDAO.Instance.HasOverlapAsync(roomId, checkIn, checkOut, excludeId);

        public Task<Room?> GetReservableRoomAsync(int roomId) => ReservationDAO.Instance.GetReservableRoomAsync(roomId);
        public Task<Guest?> GetGuestAsync(int guestId) => ReservationDAO.Instance.GetGuestAsync(guestId);
        public Task<bool> BookingCodeExistsAsync(string code) => ReservationDAO.Instance.BookingCodeExistsAsync(code);
        public Task<int> CreateAsync(Reservation reservation) => ReservationDAO.Instance.CreateAsync(reservation);

        public Task UpdateAsync(int id, int guestId, int roomId, int numberOfGuests,
            DateTime checkIn, DateTime checkOut, ReservationStatus status, string? specialRequests)
            => ReservationDAO.Instance.UpdateAsync(id, guestId, roomId, numberOfGuests, checkIn, checkOut, status, specialRequests);

        public Task SetStatusAsync(int id, ReservationStatus status) => ReservationDAO.Instance.SetStatusAsync(id, status);
    }
}
