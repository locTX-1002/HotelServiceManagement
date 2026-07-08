using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Application.DTOs.Rooms;

public class CreateRoomRequest
{
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public int RoomTypeId { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public bool IsActive { get; set; } = true;
}
