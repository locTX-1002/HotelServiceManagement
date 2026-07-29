using BusinessObjects;
using BusinessObjects.Entities;
using Services;

namespace FUHotelManagementWPF.ViewModels.Users
{
    public class UserRow
    {
        public User User { get; }
        public UserRow(User user) => User = user;

        public string Initial => string.IsNullOrWhiteSpace(User.FullName) ? "?" : User.FullName.Trim()[..1].ToUpper();
        public string FullName => User.FullName;
        public string Email => User.Email;
        public string RoleName => User.Role?.RoleName ?? string.Empty;

        public string RoleDisplay => RoleName switch
        {
            RoleNames.Admin => "Quản trị viên",
            RoleNames.Manager => "Quản lý",
            RoleNames.Receptionist => "Lễ tân",
            RoleNames.ServiceStaff => "Nhân viên dịch vụ",
            RoleNames.Guest => "Khách hàng",
            _ => "Chưa gán vai trò",
        };

        public bool IsActive => User.IsActive;
        public string StatusText => User.IsActive ? "Đang hoạt động" : "Đã khoá";
        public bool IsSelf => AppSession.CurrentUser?.Id == User.Id;
        public bool IsAdminAccount => RoleName == RoleNames.Admin;
        public bool IsGuestAccount => RoleName == RoleNames.Guest;

        public bool CanEdit => !IsAdminAccount && !IsGuestAccount;
        public bool CanResetPassword => !IsAdminAccount && !IsGuestAccount;
        public bool CanToggleActive => !IsSelf && !IsAdminAccount;

        public bool ShowAdminLockNote => IsAdminAccount;
        public bool ShowGuestNote => IsGuestAccount;

        public string ToggleActiveText => User.IsActive ? "Khoá tài khoản" : "Mở khoá";

        public string ToggleActiveToolTip => IsAdminAccount
            ? "Tài khoản Quản trị viên không thao tác được từ ứng dụng"
            : IsSelf
                ? "Không thể tự khoá tài khoản đang đăng nhập"
                : IsGuestAccount
                    ? (User.IsActive ? "Khoá tài khoản khách hàng" : "Mở khoá tài khoản khách hàng")
                    : (User.IsActive ? "Khoá nhân viên - họ sẽ không đăng nhập được nữa" : "Mở khoá để nhân viên đăng nhập lại");
    }
}
