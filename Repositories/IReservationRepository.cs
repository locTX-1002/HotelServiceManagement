using BusinessObjects.Entities;

namespace Repositories;

public interface IReservationRepository
{
    Task<List<Reservation>> GetAllAsync();
    Task<Reservation?> GetByIdAsync(int id);
    Task<bool> BookingCodeExistsAsync(string code);
    Task<bool> HasOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeId = null);
    Task AddAsync(Reservation entity);
    Task UpdateAsync(Reservation entity);
    Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut);
}
