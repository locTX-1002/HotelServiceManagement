using BusinessObjects.Entities;
using Services;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>Dong hien thi phong cho ca so do lan bang - bind thang, khong can converter.</summary>
    public class RoomRow
    {
        public Room Room { get; }
        public string StatusText { get; }

        /// <summary>Tieu de nhom tren so do (VD "TẦNG 1 · STANDARD") - VM gan sau khi load ca tang.</summary>
        public string GroupTitle { get; set; } = string.Empty;

        public string TypeName => Room.RoomType?.TypeName ?? string.Empty;
        public int Capacity => Room.RoomType?.Capacity ?? 0;
        public decimal BasePrice => Room.RoomType?.BasePrice ?? 0;
        public string ActiveText => Room.IsActive ? "Đang dùng" : "Ngừng dùng";

        /// <summary>Dong phu tren card-row: "Standard · Tầng 1 · 2 khách".</summary>
        public string SubText => $"{TypeName} · Tầng {Room.Floor} · {Capacity} khách";
        public string PriceText => $"{BasePrice:N0} đ/đêm";

        // Cho panel chi tiet (D3 master-detail)
        public string FloorText => $"Tầng {Room.Floor}";
        public string CapacityText => $"{Capacity} khách";
        public string? DescriptionText => Room.RoomType?.Description;
        public bool HasDescription => !string.IsNullOrWhiteSpace(DescriptionText);

        /// <summary>Thumbnail theo LOAI phong - xem RoomImages de biet cach map/thay anh.</summary>
        public string ThumbnailSource => RoomImages.Thumbnail(TypeName);

        public RoomRow(Room room)
        {
            Room = room;
            StatusText = RoomService.RoomStatusText(room.Status);
        }
    }

    /// <summary>Lua chon trang thai cho combobox/danh sach.</summary>
    public record StatusOption(BusinessObjects.Enums.RoomStatus Status, string Label);
}
