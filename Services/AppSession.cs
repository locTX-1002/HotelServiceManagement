using BusinessObjects.Entities;

namespace Services
{
    /// <summary>
    /// Phien dang nhap cua app desktop - thay the JWT cua ban web. Song trong process,
    /// dong app la het phien; moi man hinh doc CurrentUser de biet ai dang thao tac va role gi.
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
