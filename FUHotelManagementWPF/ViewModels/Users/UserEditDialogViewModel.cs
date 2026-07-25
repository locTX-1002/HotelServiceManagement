using System;
using System.Collections.Generic;
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
        /// IUserManagementService khong co ham lay danh sach Role nen phai ghi cung 4 vai tro
        /// o day. Id lay dung theo seed trong DataAccessObjects/Configurations/RoleConfiguration.cs
        /// (1=Admin, 2=Manager, 3=Receptionist, 4=ServiceStaff) - doi seed thi phai sua theo.
        /// </summary>
        public List<RoleOption> RoleOptions { get; } =
        [
            new(1, "Quản trị viên (Admin)"),
            new(2, "Quản lý (Manager)"),
            new(3, "Lễ tân (Receptionist)"),
            new(4, "Nhân viên dịch vụ (ServiceStaff)"),
        ];

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

        private RoleOption _selectedRole;
        public RoleOption SelectedRole
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
            _selectedRole = RoleOptions[2]; // Le tan - vai tro hay tao nhat

            if (existing != null)
            {
                _fullName = existing.FullName;
                _email = existing.Email;
                _selectedRole = RoleOptions.FirstOrDefault(o => o.Id == existing.RoleId) ?? RoleOptions[2];
            }
        }

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
            if (string.IsNullOrWhiteSpace(Email))
            {
                AddError(nameof(Email), "Chưa nhập email.");
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
                return;
            }

            IsBusy = true;
            try
            {
                var result = IsEdit
                    ? await _service.UpdateAsync(_existing!.Id, FullName, Email, SelectedRole.Id)
                    : await _service.CreateAsync(FullName, Email, password, SelectedRole.Id);

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
