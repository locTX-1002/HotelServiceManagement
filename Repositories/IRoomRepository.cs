using BusinessObjects.Entities;

namespace Repositories
{
    public interface IRoomRepository
    {
        Task<List<Room>> GetAllAsync();
        Task<Room?> GetByIdAsync(int id);
        Task<bool> RoomNumberExistsAsync(string roomNumber, int? excludeId = null);
        Task<bool> HasActiveStayAsync(int roomId);
        Task<bool> HasCheckedInReservationAsync(int roomId);
        Task<bool> HasPendingOrConfirmedReservationAsync(int roomId);
        Task<bool> HasReservationExceedingCapacityAsync(int roomId, int capacity);
        Task<bool> HasAnyReservationAsync(int roomId);
        Task AddAsync(Room room);
        Task UpdateAsync(Room room);
        Task DeleteAsync(Room room);
    }
}
