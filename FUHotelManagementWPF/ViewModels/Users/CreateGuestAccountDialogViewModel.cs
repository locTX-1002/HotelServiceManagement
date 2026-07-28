using System;
using System.Threading.Tasks;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Users
{
    /// <summary>
    /// Admin tao tai khoan DANG NHAP cho KHACH ngay tu man Nguoi dung (khong phai nhan vien).
    /// Khach dang nhap bang so dien thoai + mat khau de tu dat phong. Neu SDT da co ho so ma
    /// chua co tai khoan, service se cap tai khoan cho ho so do (khong tao ho so trung).
    /// </summary>
    public class CreateGuestAccountDialogViewModel : ValidatableViewModelBase
    {
        private readonly IGuestAccountService _service = new GuestAccountService();

        public event Action<bool>? RequestClose;

        public string Title => "Tạo tài khoản khách";
        public string Subtitle => "Khách đăng nhập bằng số điện thoại và mật khẩu để tự đặt phòng.";

        // PasswordPolicy chi tra ve cau loi khi sai, khong expose mo ta - viet tay giong
        // UserEditDialogViewModel. Sua PasswordPolicy.Validate thi sua ca dong nay.
        public string PasswordHint =>
            "Ít nhất 8 ký tự, có chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Ví dụ: Hotel@2026";

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set { if (SetProperty(ref _fullName, value)) { ValidateFullName(); } }
        }

        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set { if (SetProperty(ref _phone, value)) { ValidatePhone(); } }
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set { if (SetProperty(ref _email, value)) { ValidateEmail(); } }
        }

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

        private void ValidateFullName()
        {
            ClearErrors(nameof(FullName));
            if (string.IsNullOrWhiteSpace(FullName)) { AddError(nameof(FullName), "Chưa nhập họ tên."); }
        }

        private void ValidatePhone()
        {
            ClearErrors(nameof(Phone));
            var error = InputPolicy.ValidatePhone(Phone, required: true);
            if (error != null) { AddError(nameof(Phone), error); }
        }

        private void ValidateEmail()
        {
            ClearErrors(nameof(Email));
            var error = InputPolicy.ValidateEmail(Email, required: false);
            if (error != null) { AddError(nameof(Email), error); }
        }

        /// <summary>Mat khau nhan tu code-behind (PasswordBox khong binding duoc).</summary>
        public async Task SaveAsync(string password, string confirmPassword)
        {
            ErrorMessage = null;
            ValidateFullName(); ValidatePhone(); ValidateEmail();
            if (HasErrors)
            {
                ErrorMessage = FirstError();
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _service.CreateByStaffAsync(FullName, Phone, Email, password, confirmPassword);
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
                ErrorMessage = "Không tạo được tài khoản. Kiểm tra kết nối SQL Server rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
