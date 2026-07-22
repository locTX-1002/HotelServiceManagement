using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels
{
    /// <summary>
    /// MAU CHUAN cho moi form cua nhom: ke thua ValidatableViewModelBase (loi theo tung o),
    /// dung AsyncRelayCommand (khong block UI, tu chong bam doi), loi nghiep vu chung
    /// hien qua ErrorMessage (banner do), loi tung field qua AddError (o tu vien do).
    /// </summary>
    public class LoginViewModel : ValidatableViewModelBase
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

        public AsyncRelayCommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new AsyncRelayCommand(DoLoginAsync, _ => !IsBusy);
        }

        private async Task DoLoginAsync(object? parameter)
        {
            // PasswordBox khong cho binding truc tiep (ly do bao mat cua WPF)
            // nen View truyen ca control qua CommandParameter.
            var password = (parameter as PasswordBox)?.Password ?? string.Empty;

            ClearAllErrors();
            ErrorMessage = null;

            var email = Email.Trim();
            if (string.IsNullOrEmpty(email))
            {
                AddError(nameof(Email), "Chưa nhập email.");
            }
            else if (!email.Contains('@'))
            {
                AddError(nameof(Email), "Email không đúng định dạng.");
            }
            if (string.IsNullOrEmpty(password))
            {
                // PasswordBox khong binding duoc nen loi mat khau di qua banner
                ErrorMessage = "Vui lòng nhập mật khẩu.";
            }
            if (HasErrors || ErrorMessage != null)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var user = await _authService.LoginAsync(email, password);
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
