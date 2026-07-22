using System.Collections.Generic;

namespace FUHotelManagementWPF.ViewModels.Rooms
{
    /// <summary>
    /// Map LOAI phong -> bo anh nhung (Assets/Rooms/{key}-{n}.jpg, 3 anh/loai).
    /// Khong can cot DB: ten loai chua "suite"/"deluxe"/"family" thi dung bo do,
    /// con lai dung standard. Thay anh that = ghi de file cung ten.
    /// </summary>
    public static class RoomImages
    {
        private const int PerType = 3;

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

        public static string Thumbnail(string typeName)
            => $"pack://application:,,,/Assets/Rooms/{TypeKey(typeName)}-1.jpg";

        public static List<string> Gallery(string typeName)
        {
            var key = TypeKey(typeName);
            var images = new List<string>(PerType);
            for (var i = 1; i <= PerType; i++)
            {
                images.Add($"pack://application:,,,/Assets/Rooms/{key}-{i}.jpg");
            }
            return images;
        }
    }
}
