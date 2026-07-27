using BusinessObjects.Entities;

namespace Repositories;

public interface IReservationRepository
{
    Task<List<Reservation>> GetAllAsync();

    /// <summary>Don dat phong cua dung mot khach - man "Đặt phòng của tôi" ben phia khach.</summary>
    Task<List<Reservation>> GetByGuestAsync(int guestId);
    Task<List<Reservation>> GetRecentByRoomAsync(int roomId, int count = 5);

    Task<Reservation?> GetByIdAsync(int id);
    Task<bool> BookingCodeExistsAsync(string code);
    Task<bool> HasOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeId = null);
    Task AddAsync(Reservation entity);
    Task UpdateAsync(Reservation entity);
    Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut);
    Task<int> SweepNoShowAsync(DateTime today);
}
