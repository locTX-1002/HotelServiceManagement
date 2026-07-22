using BusinessObjects.Entities;
using Services;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>Dong hien thi phong cho ca so do lan bang - bind thang, khong can converter.</summary>
    public class RoomRow
    {
        public Room Room { get; }
        public string StatusText { get; }

        public string TypeName => Room.RoomType?.TypeName ?? string.Empty;
        public int Capacity => Room.RoomType?.Capacity ?? 0;
        public decimal BasePrice => Room.RoomType?.BasePrice ?? 0;
        public string ActiveText => Room.IsActive ? "Đang dùng" : "Ngừng dùng";

        public RoomRow(Room room)
        {
            Room = room;
            StatusText = RoomService.RoomStatusText(room.Status);
        }
    }

    /// <summary>Lua chon trang thai cho combobox/danh sach.</summary>
    public record StatusOption(BusinessObjects.Enums.RoomStatus Status, string Label);
}
