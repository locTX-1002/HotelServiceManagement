using BusinessObjects;
using BusinessObjects.Entities;
using Services;

namespace FUHotelManagementWPF.ViewModels.Users
{
    /// <summary>
    /// Dong tai khoan cho card-row + panel chi tiet. Boc User lai de View khong phai
    /// tu ghep chuoi (vai tro trong DB la tieng Anh, man hinh phai hien tieng Viet).
    /// </summary>
    public class UserRow
    {
        public User User { get; }

        public UserRow(User user) => User = user;

        public string Initial => string.IsNullOrWhiteSpace(User.FullName)
            ? "?" : User.FullName.Trim()[..1].ToUpper();

        public string FullName => User.FullName;
        public string Email => User.Email;

        /// <summary>Ten vai tro goc trong DB - dung cho loc va cho DataTrigger doi mau badge.</summary>
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

        /// <summary>
        /// Chinh minh dang dang nhap. Dung de an nut khoa - neu tu khoa minh thi
        /// lan dang nhap sau khong ai vao duoc bang Admin nua (service cung chan).
        /// </summary>
        public bool IsSelf => AppSession.CurrentUser?.Id == User.Id;

        /// <summary>
        /// Tai khoan Quan tri vien khong sua / khoa / doi mat khau qua giao dien duoc.
        /// No do cau hinh trien khai tao ra; cho sua trong app thi mot Admin co the tu
        /// nhan ban quyen cao nhat hoac tu khoa minh ra ngoai, khong con duong thu hoi.
        /// Service cung chan doc lap - day chi la lop an nut cho do bam nham.
        /// </summary>
        public bool IsAdminAccount => RoleName == RoleNames.Admin;

        /// <summary>
        /// Tai khoan Khach hang (GuestAccount) khong nam trong bang User - khong sua/khoa/
        /// doi mat khau qua UserManagementService duoc. Cac thao tac nay chi danh cho nhan vien.
        /// </summary>
        public bool IsGuestAccount => RoleName == RoleNames.Guest;

        public bool CanEdit => !IsAdminAccount && !IsGuestAccount;
        public bool CanResetPassword => !IsAdminAccount && !IsGuestAccount;
        public bool CanToggleActive => !IsSelf && !IsAdminAccount && !IsGuestAccount;

        /// <summary>Hien dong giai thich thay cho cac nut da an.</summary>
        public bool ShowAdminLockNote => IsAdminAccount;
        public bool ShowGuestNote => IsGuestAccount;

        public string ToggleActiveText => User.IsActive ? "Khoá tài khoản" : "Mở khoá";

        public string ToggleActiveToolTip => IsAdminAccount
            ? "Tài khoản Quản trị viên không thao tác được từ ứng dụng"
            : IsGuestAccount
                ? "Tài khoản khách hàng không khoá từ đây"
                : IsSelf
                    ? "Không thể tự khoá tài khoản đang đăng nhập"
                    : User.IsActive
                        ? "Khoá tài khoản - nhân viên này sẽ không đăng nhập được nữa"
                        : "Mở khoá để nhân viên đăng nhập lại";
    }
}
