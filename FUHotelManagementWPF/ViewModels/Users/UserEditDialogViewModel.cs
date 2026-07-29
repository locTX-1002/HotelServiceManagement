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
    /// <summary>Một lựa chọn vai trò trong ComboBox (Id khớp bảng Roles trong DB).</summary>
    public record RoleOption(int Id, string Label);

    /// <summary>
    /// Dialog thêm / sửa tài khoản nhân viên. Mật khẩu KHÔNG nằm trong ViewModel:
    /// code-behind đọc thẳng từ PasswordBox rồi truyền vào SaveAsync.
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
        /// Vai trò ĐỌC TỪ DATABASE (bảng Roles).
        /// Danh sách này KHÔNG có Admin & Guest - chỉ dùng gán vai trò nhân viên.
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

        private bool _isPasswordVisible;
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
        }

        public RelayCommand TogglePasswordCommand { get; }

        public UserEditDialogViewModel(User? existing)
        {
            _existing = existing;
            TogglePasswordCommand = new RelayCommand(_ => IsPasswordVisible = !IsPasswordVisible);

            if (existing != null)
            {
                _fullName = existing.FullName;
                _email = existing.Email;

                // Nếu là tài khoản Khách hàng thì cảnh báo
                if (existing.Role?.RoleName == RoleNames.Guest)
                {
                    ErrorMessage = "Tài khoản Khách hàng không chỉnh sửa tại đây. Hãy sử dụng chức năng Đặt lại mật khẩu hoặc Khoá tài khoản.";
                }
            }

            _ = LoadRolesAsync();
        }

        /// <summary>Đổ vai trò từ DB vào ComboBox rồi chọn sẵn đúng vai trò đang có.</summary>
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
                // Không hiển thị vai trò Guest trong danh sách gán vai trò nhân viên
                if (role.RoleName != RoleNames.Guest)
                {
                    RoleOptions.Add(new RoleOption(role.Id, Describe(role.RoleName)));
                }
            }

            SelectedRole = _existing != null
                ? RoleOptions.FirstOrDefault(o => o.Id == _existing.RoleId)
                : RoleOptions.FirstOrDefault(o => o.Label.Contains("Lễ tân")) ?? RoleOptions.FirstOrDefault();
        }

        /// <summary>Tên vai trò trong DB là tiếng Anh; màn hình phải hiện tiếng Việt.</summary>
        private static string Describe(string roleName) => roleName switch
        {
            RoleNames.Manager => "Quản lý (Manager)",
            RoleNames.Receptionist => "Lễ tân (Receptionist)",
            RoleNames.ServiceStaff => "Nhân viên dịch vụ (ServiceStaff)",
            RoleNames.Guest => "Khách hàng (Guest)",
            _ => roleName,
        };

        public async Task SaveAsync(string password, string confirmPassword)
        {
            if (IsBusy) return;

            // Ngăn chặn lưu nếu là tài khoản Khách hàng
            if (_existing?.Role?.RoleName == RoleNames.Guest)
            {
                ErrorMessage = "Tài khoản Khách hàng không chỉnh sửa tại đây. Vui lòng sử dụng Đặt lại mật khẩu hoặc Khoá tài khoản.";
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