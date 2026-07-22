using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories
{
    public class RoomTypeRepository : IRoomTypeRepository
    {
        public Task<List<RoomType>> GetAllAsync() => RoomTypeDAO.Instance.GetAllAsync();
        public Task<List<RoomType>> GetActiveAsync() => RoomTypeDAO.Instance.GetActiveAsync();
        public Task<RoomType?> GetByIdAsync(int id) => RoomTypeDAO.Instance.GetByIdAsync(id);

        public Task<bool> NameExistsAsync(string typeName, int? excludeId = null)
            => RoomTypeDAO.Instance.NameExistsAsync(typeName, excludeId);

        public Task<bool> HasReservationExceedingCapacityAsync(int roomTypeId, int newCapacity)
            => RoomTypeDAO.Instance.HasReservationExceedingCapacityAsync(roomTypeId, newCapacity);

        public Task AddAsync(RoomType roomType) => RoomTypeDAO.Instance.AddAsync(roomType);
        public Task UpdateAsync(RoomType roomType) => RoomTypeDAO.Instance.UpdateAsync(roomType);
        public Task DeleteAsync(RoomType roomType) => RoomTypeDAO.Instance.DeleteAsync(roomType);
    }
}
