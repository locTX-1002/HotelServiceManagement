using BusinessObjects.Entities;

namespace Services
{
    /// <summary>
    /// Phien dang nhap cua ung dung desktop. Phien chi ton tai trong process;
    /// moi man hinh doc CurrentUser de biet nguoi dang thao tac va vai tro hien tai.
    /// </summary>
    public static class AppSession
    {
        public static User? CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;
        public static string RoleName => CurrentUser?.Role?.RoleName ?? string.Empty;

        public static void SignIn(User user) => CurrentUser = user;
        public static void SignOut() => CurrentUser = null;
    }
}
