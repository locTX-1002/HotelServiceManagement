using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// Nho tai khoan dang nhap giua cac lan mo app.
    ///
    /// File nam trong %APPDATA% chu KHONG nam trong thu muc repo. De trong repo thi
    /// hoac bi commit len cho ca nhom thay mat khau, hoac chung so phan
    /// appsettings.Local.json: git doi nhanh mot cai la mat sach.
    ///
    /// Mat khau ma hoa bang DPAPI theo tai khoan Windows dang dang nhap, nen chep
    /// file sang may khac hay sang user khac thi giai khong ra.
    /// </summary>
    public static class RememberedLogin
    {
        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FUHotelManagement", "login.json");

        private sealed class Stored
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public static (string Email, string Password)? Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return null;
                }
                var stored = JsonSerializer.Deserialize<Stored>(File.ReadAllText(FilePath));
                if (stored == null || string.IsNullOrWhiteSpace(stored.Email))
                {
                    return null;
                }
                return (stored.Email, Unprotect(stored.Password));
            }
            catch (Exception)
            {
                // File hong, hoac chep tu may khac nen DPAPI giai khong ra.
                // Coi nhu chua nho gi - khong duoc de chuyen nay chan man dang nhap.
                return null;
            }
        }

        public static void Save(string email, string password)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(new Stored
                {
                    Email = email,
                    Password = Protect(password),
                }));
            }
            catch (Exception)
            {
                // Khong ghi duoc thi thoi, dang nhap van thanh cong binh thuong.
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch (Exception)
            {
            }
        }

        private static string Protect(string value)
            => Convert.ToBase64String(ProtectedData.Protect(
                Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));

        private static string Unprotect(string value)
            => string.IsNullOrEmpty(value)
                ? string.Empty
                : Encoding.UTF8.GetString(ProtectedData.Unprotect(
                    Convert.FromBase64String(value), null, DataProtectionScope.CurrentUser));
    }
}
