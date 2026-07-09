namespace HotelServiceManagement.Application.DTOs.Rooms;

public class RoomMapFloorResponse
{
    public int Floor { get; set; }
    public IReadOnlyList<RoomResponse> Rooms { get; set; } = Array.Empty<RoomResponse>();
}
