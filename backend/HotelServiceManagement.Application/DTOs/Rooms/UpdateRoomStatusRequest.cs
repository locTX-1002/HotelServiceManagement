using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Application.DTOs.Rooms
{
    public class UpdateRoomStatusRequest
    {
        public RoomStatus Status { get; set; }
    }
}
