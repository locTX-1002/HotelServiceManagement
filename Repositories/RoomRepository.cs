using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories
{
    public class RoomRepository : IRoomRepository
    {
        public Task<List<Room>> GetAllAsync() => RoomDAO.Instance.GetAllAsync();
        public Task<Room?> GetByIdAsync(int id) => RoomDAO.Instance.GetByIdAsync(id);

        public Task<bool> RoomNumberExistsAsync(string roomNumber, int? excludeId = null)
            => RoomDAO.Instance.RoomNumberExistsAsync(roomNumber, excludeId);

        public Task<bool> HasActiveStayAsync(int roomId)
            => RoomDAO.Instance.HasActiveStayAsync(roomId);

        public Task<bool> HasCheckedInReservationAsync(int roomId)
            => RoomDAO.Instance.HasCheckedInReservationAsync(roomId);

        public Task<bool> HasPendingOrConfirmedReservationAsync(int roomId)
            => RoomDAO.Instance.HasPendingOrConfirmedReservationAsync(roomId);

        public Task<bool> HasReservationExceedingCapacityAsync(int roomId, int capacity)
            => RoomDAO.Instance.HasReservationExceedingCapacityAsync(roomId, capacity);

        public Task<bool> HasAnyReservationAsync(int roomId)
            => RoomDAO.Instance.HasAnyReservationAsync(roomId);

        public Task AddAsync(Room room) => RoomDAO.Instance.AddAsync(room);
        public Task UpdateAsync(Room room) => RoomDAO.Instance.UpdateAsync(room);
        public Task DeleteAsync(Room room) => RoomDAO.Instance.DeleteAsync(room);
    }
}
