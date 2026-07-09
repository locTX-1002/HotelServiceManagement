using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Rooms;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IRoomService
    {
        Task<AuthServiceResult<IReadOnlyList<RoomResponse>>> GetAllAsync();
        Task<AuthServiceResult<IReadOnlyList<RoomMapFloorResponse>>> GetMapAsync();
        Task<AuthServiceResult<RoomResponse>> GetByIdAsync(int id);
        Task<AuthServiceResult<RoomResponse>> CreateAsync(CreateRoomRequest request);
        Task<AuthServiceResult<RoomResponse>> UpdateAsync(int id, UpdateRoomRequest request);
        Task<AuthServiceResult<AuthMessageResponse>> DeleteAsync(int id);
    }
}
