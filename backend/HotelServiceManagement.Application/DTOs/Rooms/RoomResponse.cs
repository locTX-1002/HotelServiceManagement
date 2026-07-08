using HotelServiceManagement.Domain.Enums;

namespace HotelServiceManagement.Application.DTOs.Rooms;

public class RoomResponse
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public RoomStatus Status { get; set; }
    public bool IsActive { get; set; }
}
