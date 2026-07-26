using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    public Task<List<Reservation>> GetAllAsync() => ReservationDAO.Instance.GetAllAsync();
    public Task<List<Reservation>> GetByGuestAsync(int guestId) => ReservationDAO.Instance.GetByGuestAsync(guestId);
    public Task<Reservation?> GetByIdAsync(int id) => ReservationDAO.Instance.GetByIdAsync(id);
    public Task<bool> BookingCodeExistsAsync(string code) => ReservationDAO.Instance.BookingCodeExistsAsync(code);
    public Task<bool> HasOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeId = null)
        => ReservationDAO.Instance.HasOverlapAsync(roomId, checkIn, checkOut, excludeId);
    public Task AddAsync(Reservation entity) => ReservationDAO.Instance.AddAsync(entity);
    public Task UpdateAsync(Reservation entity) => ReservationDAO.Instance.UpdateAsync(entity);
    public Task<int> SweepNoShowAsync(DateTime today) => ReservationDAO.Instance.SweepNoShowAsync(today);

    public Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut)
        => ReservationDAO.Instance.GetAvailableRoomsAsync(checkIn, checkOut);
}
