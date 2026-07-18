namespace HotelServiceManagement.Application.DTOs.Reservations;

// Khach chon LOAI phong, khong chon so phong cu the - gom nhom ket qua GetAvailableRoomsAsync theo
// RoomTypeId de hien 1 the/loai phong kem so luong con trong, giong dung UX cac trang booking thong thuong.
public class AvailableRoomTypeResponse
{
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public int AvailableCount { get; set; }
}
