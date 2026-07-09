using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.RoomTypes;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IRoomTypeService
    {
        Task<AuthServiceResult<IReadOnlyList<RoomTypeResponse>>> GetAllAsync();
        Task<AuthServiceResult<RoomTypeResponse>> GetByIdAsync(int id);
        Task<AuthServiceResult<RoomTypeResponse>> CreateAsync(CreateRoomTypeRequest request);
        Task<AuthServiceResult<RoomTypeResponse>> UpdateAsync(int id, UpdateRoomTypeRequest request);
        Task<AuthServiceResult<AuthMessageResponse>> DeleteAsync(int id);
    }
}
