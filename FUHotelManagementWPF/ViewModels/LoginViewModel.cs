using System;
using System.Windows.Controls;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService = new AuthService();

        private string _email = string.Empty;
        private string? _errorMessage;
        private bool _isBusy;

        /// <summary>View lang nghe de mo MainWindow va dong cua so login.</summary>
        public event Action? LoginSucceeded;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public RelayCommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(DoLogin, _ => !IsBusy);
        }

        private void DoLogin(object? parameter)
        {
            // PasswordBox khong cho binding truc tiep (ly do bao mat cua WPF)
            // nen View truyen ca control qua CommandParameter.
            var password = (parameter as PasswordBox)?.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ email và mật khẩu.";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;
            try
            {
                var user = _authService.Login(Email.Trim(), password);
                if (user == null)
                {
                    ErrorMessage = "Email hoặc mật khẩu không đúng.";
                    return;
                }

                AppSession.SignIn(user);
                LoginSucceeded?.Invoke();
            }
            catch (Exception)
            {
                ErrorMessage = "Không kết nối được cơ sở dữ liệu. Kiểm tra SQL Server rồi thử lại.";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
