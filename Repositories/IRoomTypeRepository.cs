using BusinessObjects.Entities;

namespace Repositories
{
    public interface IRoomTypeRepository
    {
        Task<List<RoomType>> GetAllAsync();
        Task<List<RoomType>> GetActiveAsync();
        Task<RoomType?> GetByIdAsync(int id);
        Task<bool> NameExistsAsync(string typeName, int? excludeId = null);
        Task<bool> HasReservationExceedingCapacityAsync(int roomTypeId, int newCapacity);
        Task AddAsync(RoomType roomType);
        Task UpdateAsync(RoomType roomType);
        Task DeleteAsync(RoomType roomType);
    }
}
