using System;
using System.Threading.Tasks;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Guests
{
    /// <summary>
    /// Dialog kich hoat tai khoan dat phong cho khach (dang nhap bang SDT).
    /// Mat khau lay truc tiep tu PasswordBox o code-behind (ngoai le MVVM chuan cua nhom,
    /// giong LoginWindow) - KHONG giu mat khau trong property/log theo quy dinh phan cong.
    /// </summary>
    public class ActivateAccountDialogViewModel : ViewModelBase
    {
        private readonly IGuestAccountService _accountService = new GuestAccountService();
        private readonly Guest _guest;

        public event Action<bool>? RequestClose;

        public string Title => $"Kích hoạt tài khoản: {_guest.FullName}";
        public string PhoneNumber => _guest.PhoneNumber;

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
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        public bool CanSave => !IsBusy;

        public ActivateAccountDialogViewModel(Guest guest) => _guest = guest;

        /// <summary>Code-behind goi khi bam Kich hoat - password khong di qua binding.</summary>
        public async Task ActivateAsync(string password, string confirm)
        {
            ErrorMessage = null;

            if (string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Chưa nhập mật khẩu.";
                return;
            }
            if (password != confirm)
            {
                ErrorMessage = "Xác nhận mật khẩu chưa khớp.";
                return;
            }

            IsBusy = true;
            try
            {
                // PasswordPolicy do service kiem tra - loi (mat khau yeu, da co tai khoan...) hien nguyen Message
                var result = await _accountService.ActivateAsync(_guest.Id, password);
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
                ErrorMessage = "Không kích hoạt được. Kiểm tra kết nối SQL Server rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
