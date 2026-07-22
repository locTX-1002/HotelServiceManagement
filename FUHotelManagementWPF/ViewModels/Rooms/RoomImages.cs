using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>
    /// Anh theo LOAI phong, 2 nguon theo thu tu uu tien:
    /// 1) Anh NGUOI DUNG tu chon (bam vao anh trong dialog -> chon file tu may),
    ///    duoc copy vao thu muc RoomImages/ canh exe - giu qua cac lan mo app.
    /// 2) Anh nhung san trong Assets/Rooms/{key}-{n}.jpg (3 anh/loai).
    /// Ten loai chua "suite"/"deluxe"/"family" thi dung bo do, con lai standard.
    /// </summary>
    public static class RoomImages
    {
        private const int PerType = 3;

        private static string RuntimeDir => Path.Combine(AppContext.BaseDirectory, "RoomImages");

        private static string TypeKey(string typeName)
        {
            var name = typeName.ToLowerInvariant();
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

        private static string? FindCustom(string key)
        {
            if (!Directory.Exists(RuntimeDir))
            {
                return null;
            }
            return Directory.GetFiles(RuntimeDir, key + "-custom-*")
                .OrderByDescending(f => f)
                .FirstOrDefault();
        }

        public static string Thumbnail(string typeName)
        {
            var key = TypeKey(typeName);
            return FindCustom(key) ?? $"pack://application:,,,/Assets/Rooms/{key}-1.jpg";
        }

        public static List<string> Gallery(string typeName)
        {
            var key = TypeKey(typeName);
            var images = new List<string>();
            var custom = FindCustom(key);
            if (custom != null)
            {
                images.Add(custom);
            }
            for (var i = 1; i <= PerType; i++)
            {
                images.Add($"pack://application:,,,/Assets/Rooms/{key}-{i}.jpg");
            }
            return images;
        }

        /// <summary>
        /// Nguoi dung chon anh tu may: copy vao RoomImages/ canh exe. Ten file kem
        /// timestamp de WPF khong cache anh cu (URI doi -> binding tu refresh).
        /// </summary>
        public static void SetCustomImage(string typeName, string sourcePath)
        {
            var key = TypeKey(typeName);
            Directory.CreateDirectory(RuntimeDir);
            foreach (var old in Directory.GetFiles(RuntimeDir, key + "-custom-*"))
            {
                File.Delete(old);
            }
            var destination = Path.Combine(
                RuntimeDir,
                $"{key}-custom-{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(sourcePath)}");
            File.Copy(sourcePath, destination);
        }
    }
}
