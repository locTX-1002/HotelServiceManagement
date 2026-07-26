using System;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Users
{
    /// <summary>
    /// Dialog nho: Admin dat lai mat khau cho mot nhan vien. Mat khau chi di qua tham so
    /// cua ResetAsync, khong luu vao property nao de khong lo ra binding/log.
    /// </summary>
    public class ResetPasswordDialogViewModel : ViewModelBase
    {
        private readonly IUserManagementService _service = new UserManagementService();
        private readonly User _user;

        public event Action<bool>? RequestClose;

        public string Title => "Đặt lại mật khẩu";
        public string UserName => _user.FullName;
        public string Email => _user.Email;

        /// <summary>
        /// PasswordPolicy khong co san mo ta yeu cau nen viet tay theo dung luat trong
        /// PasswordPolicy.Validate - sua luat thi sua ca dong nay.
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

        public ResetPasswordDialogViewModel(User user) => _user = user;

        /// <summary>Hai chuoi do code-behind doc tu PasswordBox truyen sang.</summary>
        public async Task ResetAsync(string password, string confirmPassword)
        {
            if (IsBusy)
            {
                return;
            }

            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Chưa nhập mật khẩu mới.";
                return;
            }
            if (password != confirmPassword)
            {
                ErrorMessage = "Hai ô mật khẩu chưa khớp nhau.";
                return;
            }

            IsBusy = true;
            try
            {
                // Service tu kiem tra do manh cua mat khau va tra ve cau loi tieng Viet.
                var result = await _service.ResetPasswordAsync(_user.Id, password);
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
                ErrorMessage = "Không đặt lại được mật khẩu. Kiểm tra kết nối SQL Server rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
