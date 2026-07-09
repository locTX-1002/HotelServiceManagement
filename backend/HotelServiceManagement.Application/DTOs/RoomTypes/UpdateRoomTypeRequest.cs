namespace HotelServiceManagement.Application.DTOs.RoomTypes;

public class UpdateRoomTypeRequest
{
    public string TypeName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
