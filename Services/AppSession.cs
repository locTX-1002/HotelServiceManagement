using BusinessObjects.Entities;

namespace Services
{
    /// <summary>
    /// Phien dang nhap cua ung dung desktop. Phien chi ton tai trong process;
    /// moi man hinh doc CurrentUser de biet nguoi dang thao tac va vai tro hien tai.
    /// </summary>
    public static class AppSession
    {
        private static HashSet<string> _permissionCodes = new(StringComparer.OrdinalIgnoreCase);

        public static User? CurrentUser { get; private set; }

        /// <summary>
        /// Phien cua KHACH tu dang nhap (dang nhap bang so dien thoai). Tach hoan toan khoi
        /// CurrentUser vi khach khong phai nhan vien: RoleName cua khach luon rong nen moi
        /// service kiem quyen theo vai tro deu tu dong chan khach lai - dung nhu mong muon.
        /// </summary>
        public static GuestAccount? CurrentGuest { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        /// <summary>Dang co khach dang nhap (khong phai nhan vien).</summary>
        public static bool IsGuestLoggedIn => CurrentGuest != null;

        /// <summary>Id ho so khach dang dang nhap - dung de chi lay du lieu cua chinh khach do.</summary>
        public static int? CurrentGuestId => CurrentGuest?.GuestId;

        public static string RoleName => CurrentUser?.Role?.RoleName ?? string.Empty;

        /// <summary>
        /// Tap quyen cua phien duoc tao tu RolePermissions da nap cung tai khoan.
        /// Khong suy dien quyen tu ten vai tro.
        /// </summary>
        public static IReadOnlySet<string> PermissionCodes => _permissionCodes;

        public static bool HasPermission(string permissionCode)
            => IsLoggedIn
               && !string.IsNullOrWhiteSpace(permissionCode)
               && _permissionCodes.Contains(permissionCode);

        /// <summary>Ten hien thi cua nguoi dang dung app (nhan vien hoac khach).</summary>
        public static string DisplayName
            => CurrentUser?.FullName ?? CurrentGuest?.Guest?.FullName ?? string.Empty;

        public static void SignIn(User user)
        {
            CurrentUser = user;
            CurrentGuest = null; // hai loai phien khong bao gio ton tai cung luc
            _permissionCodes = user.Role?.RolePermissions
                .Where(x => x.IsAllowed && x.Permission.IsActive)
                .Select(x => x.Permission.PermissionCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public static void SignInGuest(GuestAccount account)
        {
            CurrentGuest = account;
            CurrentUser = null;
            _permissionCodes.Clear();
        }

        public static void SignOut()
        {
            CurrentUser = null;
            CurrentGuest = null;
            _permissionCodes.Clear();
        }
    }
}
