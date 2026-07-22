using BusinessObjects.Entities;

namespace Services
{
    public interface IRoomTypeService
    {
        Task<List<RoomType>> GetAllAsync();
        Task<List<RoomType>> GetActiveAsync();
        Task<ServiceResult<RoomType>> CreateAsync(
            string typeName, int capacity, decimal basePrice, string? description, bool isActive);
        Task<ServiceResult<RoomType>> UpdateAsync(
            int id, string typeName, int capacity, decimal basePrice, string? description, bool isActive);
        Task<ServiceResult> DeleteAsync(int id);
    }
}
