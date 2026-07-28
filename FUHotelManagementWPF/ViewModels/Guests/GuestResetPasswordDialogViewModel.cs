using System;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Guests
{
    public class GuestResetPasswordDialogViewModel : ViewModelBase
    {
        private readonly IGuestAccountService _service = new GuestAccountService();
        private readonly Guest _guest;

        public event Action<bool>? RequestClose;

        public string Title => "Đặt lại mật khẩu khách hàng";
        public string GuestName => _guest.FullName;
        public string PhoneNumber => _guest.PhoneNumber;

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

        public GuestResetPasswordDialogViewModel(Guest guest)
        {
            _guest = guest;
            TogglePasswordCommand = new RelayCommand(_ => IsPasswordVisible = !IsPasswordVisible);
        }

        public async Task ResetAsync(string password, string confirmPassword)
        {
            if (IsBusy) return;

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
                var result = await _service.ResetPasswordByAdminAsync(_guest.Id, password);
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
