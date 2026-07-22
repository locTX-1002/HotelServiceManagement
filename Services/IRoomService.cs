using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Services
{
    public interface IRoomService
    {
        Task<List<Room>> GetAllAsync();
        Task<ServiceResult<Room>> CreateAsync(
            string roomNumber, int floor, int roomTypeId, RoomStatus status, bool isActive);
        Task<ServiceResult<Room>> UpdateAsync(
            int id, string roomNumber, int floor, int roomTypeId, RoomStatus status, bool isActive);
        /// <summary>Doi trang thai van hanh (don phong/bao tri). canManageMaintenance = Admin/Manager.</summary>
        Task<ServiceResult<Room>> UpdateStatusAsync(int id, RoomStatus newStatus, bool canManageMaintenance);
        Task<ServiceResult> DeleteAsync(int id);
    }
}
