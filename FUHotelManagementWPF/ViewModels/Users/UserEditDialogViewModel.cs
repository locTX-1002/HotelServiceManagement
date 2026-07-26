using BusinessObjects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Users
{
    /// <summary>Mot lua chon vai tro trong ComboBox (Id khop bang Roles trong DB).</summary>
    public record RoleOption(int Id, string Label);

    /// <summary>
    /// Dialog them / sua tai khoan nhan vien. Mat khau KHONG nam trong ViewModel:
    /// code-behind doc thang tu PasswordBox roi truyen vao SaveAsync (xem chu thich duoi).
    /// </summary>
    public class UserEditDialogViewModel : ValidatableViewModelBase
    {
        private readonly IUserManagementService _service = new UserManagementService();
        private readonly User? _existing;

        public event Action<bool>? RequestClose;

        public bool IsEdit => _existing != null;
        public bool IsCreate => _existing == null;

        public string Title => IsEdit ? $"Sửa tài khoản {_existing!.FullName}" : "Thêm tài khoản nhân viên";
        public string Subtitle => IsEdit
            ? "Đổi họ tên, email hoặc vai trò. Mật khẩu đổi ở chức năng Đặt lại mật khẩu."
            : "Tạo tài khoản đăng nhập cho nhân viên mới.";

        /// <summary>
        /// Vai tro DOC TU DATABASE (bang Roles), khong ghi cung nua: truoc day 4 vai tro
        /// va Id nam cung trong file nay nen doi seed la lech ma khong ai biet.
        /// Danh sach nay KHONG co Admin - vai tro Quan tri vien khong duoc gan qua giao dien.
        /// </summary>
        public ObservableCollection<RoleOption> RoleOptions { get; } = [];

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private RoleOption? _selectedRole;
        public RoleOption? SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        /// <summary>
        /// Mo ta yeu cau mat khau. PasswordPolicy chi tra ve cau loi khi sai chu khong
        /// expose mo ta nen phai viet tay - sua PasswordPolicy.Validate thi sua ca dong nay.
        /// </summary>
        public string PasswordHint =>
            "Ít nhất 8 ký tự, có chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Ví dụ: Hotel@2026";

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public UserEditDialogViewModel(User? existing)
        {
            _existing = existing;

            if (existing != null)
            {
                _fullName = existing.FullName;
                _email = existing.Email;
            }

            _ = LoadRolesAsync();
        }

        /// <summary>Do vai tro tu DB vao ComboBox roi chon san dung vai tro dang co.</summary>
        private async Task LoadRolesAsync()
        {
            var result = await _service.GetAssignableRolesAsync();
            if (!result.Ok || result.Data == null)
            {
                ErrorMessage = result.Message;
                return;
            }

            RoleOptions.Clear();
            if (_existing?.Role?.RoleName == RoleNames.Admin)
            {
                RoleOptions.Add(new RoleOption(_existing.RoleId, "Quản trị viên (Admin)"));
            }
            foreach (var role in result.Data)
            {
                RoleOptions.Add(new RoleOption(role.Id, Describe(role.RoleName)));
            }

            SelectedRole = _existing != null
                ? RoleOptions.FirstOrDefault(o => o.Id == _existing.RoleId)
                : RoleOptions.FirstOrDefault(o => o.Label.Contains("Lễ tân")) ?? RoleOptions.FirstOrDefault();
        }

        /// <summary>Ten vai tro trong DB la tieng Anh; man hinh phai hien tieng Viet.</summary>
        private static string Describe(string roleName) => roleName switch
        {
            RoleNames.Manager => "Quản lý (Manager)",
            RoleNames.Receptionist => "Lễ tân (Receptionist)",
            RoleNames.ServiceStaff => "Nhân viên dịch vụ (ServiceStaff)",
            _ => roleName,
        };

        /// <summary>
        /// Luu. Hai chuoi mat khau do code-behind doc tu PasswordBox dua sang (PasswordBox
        /// khong binding duoc) - o che do sua thi bo qua ca hai.
        /// </summary>
        public async Task SaveAsync(string password, string confirmPassword)
        {
            if (IsBusy)
            {
                return;
            }

            ClearAllErrors();
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(FullName))
            {
                AddError(nameof(FullName), "Chưa nhập họ tên.");
            }
            var emailError = InputPolicy.ValidateEmail(Email, required: true);
            if (emailError != null)
            {
                AddError(nameof(Email), emailError);
            }
            if (SelectedRole == null)
            {
                ErrorMessage = "Chưa chọn vai trò.";
            }
            if (IsCreate)
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    ErrorMessage = "Chưa nhập mật khẩu.";
                }
                else if (password != confirmPassword)
                {
                    ErrorMessage = "Hai ô mật khẩu chưa khớp nhau.";
                }
            }
            if (HasErrors || ErrorMessage != null)
            {
                // Vien do khong noi duoc vi sao - day cau loi len banner cho nhin thay ngay.
                ErrorMessage ??= FirstError();
                return;
            }

            IsBusy = true;
            try
            {
                var result = IsEdit
                    ? await _service.UpdateAsync(_existing!.Id, FullName, Email, SelectedRole!.Id)
                    : await _service.CreateAsync(FullName, Email, password, SelectedRole!.Id);

                if (result.Ok)
                {
                    Notify.Success(result.Message);
                    RequestClose?.Invoke(true);
                }
                else
                {
                    // Giu nguyen form de nguoi dung sua tiep, chi hien banner loi.
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không lưu được. Kiểm tra kết nối SQL Server rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
