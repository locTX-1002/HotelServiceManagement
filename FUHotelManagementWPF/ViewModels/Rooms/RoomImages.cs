using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>
    /// Ảnh của loại phòng, 2 nguồn theo thứ tự ưu tiên:
    /// 1) Ảnh người dùng tự chọn — lưu THEO ID loại phòng (RoomImages/type-{id}-*.jpg cạnh exe).
    ///    Dùng Id chứ không dùng tên: loại mới đặt tên bất kỳ vẫn có ảnh riêng, và đổi ảnh loại này
    ///    không đụng loại khác.
    /// 2) Ảnh mẫu nhúng sẵn theo TÊN (standard/deluxe/suite/family) cho dữ liệu seed.
    /// </summary>
    public static class RoomImages
    {
        private const int PerType = 3;

        private static string RuntimeDir => Path.Combine(AppContext.BaseDirectory, "RoomImages");

        private static string SeedKey(string typeName)
        {
            var name = (typeName ?? string.Empty).ToLowerInvariant();
            if (name.Contains("suite"))
            {
                return "suite";
            }
            if (name.Contains("deluxe"))
            {
                return "deluxe";
            }
            if (name.Contains("family") || name.Contains("gia đình"))
            {
                return "family";
            }
            return "standard";
        }

        private static string? FindCustom(int roomTypeId)
        {
            if (roomTypeId <= 0 || !Directory.Exists(RuntimeDir))
            {
                return null;
            }
            return Directory.GetFiles(RuntimeDir, $"type-{roomTypeId}-*")
                .OrderByDescending(f => f)
                .FirstOrDefault();
        }

        public static string Thumbnail(int roomTypeId, string typeName)
            => FindCustom(roomTypeId) ?? $"pack://application:,,,/Assets/Rooms/{SeedKey(typeName)}-1.jpg";

        public static List<string> Gallery(int roomTypeId, string typeName)
        {
            var images = new List<string>();
            var custom = FindCustom(roomTypeId);
            if (custom != null)
            {
                images.Add(custom);
            }
            var key = SeedKey(typeName);
            for (var i = 1; i <= PerType; i++)
            {
                images.Add($"pack://application:,,,/Assets/Rooms/{key}-{i}.jpg");
            }
            return images;
        }

        /// <summary>
        /// Gán ảnh người dùng chọn cho MỘT loại phòng cụ thể. Tên file kèm timestamp để WPF
        /// không cache ảnh cũ (URI đổi → binding tự vẽ lại).
        /// </summary>
        public static void SetCustomImage(int roomTypeId, string sourcePath)
        {
            if (roomTypeId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roomTypeId));
            }
            Directory.CreateDirectory(RuntimeDir);
            foreach (var old in Directory.GetFiles(RuntimeDir, $"type-{roomTypeId}-*"))
            {
                File.Delete(old);
            }
            var destination = Path.Combine(
                RuntimeDir,
                $"type-{roomTypeId}-{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(sourcePath)}");
            File.Copy(sourcePath, destination);
        }
    }
}
