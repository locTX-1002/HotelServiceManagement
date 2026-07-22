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

        /// <summary>
        /// Thumbnail theo LOAI phong (khong can anh rieng tung phong): map ten loai -> anh nhung.
        /// Thay anh that: bo file vao Assets/Rooms/ cung ten la xong, khong dung code.
        /// </summary>
        public string ThumbnailSource
        {
            get
            {
                var name = TypeName.ToLowerInvariant();
                if (name.Contains("suite"))
                {
                    return "pack://application:,,,/Assets/Rooms/suite.png";
                }
                if (name.Contains("deluxe"))
                {
                    return "pack://application:,,,/Assets/Rooms/deluxe.png";
                }
                return "pack://application:,,,/Assets/Rooms/standard.png";
            }
        }

        public RoomRow(Room room)
        {
            Room = room;
            StatusText = RoomService.RoomStatusText(room.Status);
        }
    }

    /// <summary>Lua chon trang thai cho combobox/danh sach.</summary>
    public record StatusOption(BusinessObjects.Enums.RoomStatus Status, string Label);
}
