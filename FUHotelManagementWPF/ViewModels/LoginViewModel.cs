using System;
using System.Threading.Tasks;
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
        private string _password = string.Empty;
        private bool _rememberMe;
        private bool _isPasswordVisible;
        private string? _errorMessage;
        private bool _isBusy;

        /// <summary>View lang nghe de mo MainWindow va dong cua so login.</summary>
        public event Action? LoginSucceeded;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        /// <summary>
        /// PasswordBox cua WPF co y khong cho binding, nen View phai chep tay gia tri
        /// vao day. Doi lai thi o "hien mat khau" va tinh nang nho dang nhap deu doc
        /// chung mot cho.
        /// </summary>
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
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
        public RelayCommand TogglePasswordCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new AsyncRelayCommand(DoLoginAsync, _ => !IsBusy);
            TogglePasswordCommand = new RelayCommand(_ => IsPasswordVisible = !IsPasswordVisible);

            var remembered = RememberedLogin.Load();
            if (remembered != null)
            {
                Email = remembered.Value.Email;
                Password = remembered.Value.Password;
                RememberMe = true;
            }
        }

        private async Task DoLoginAsync(object? parameter)
        {
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
            if (string.IsNullOrEmpty(Password))
            {
                // O mat khau khi dang an la PasswordBox - khong binding duoc nen khong
                // to vien do theo Validation duoc, loi phai di qua banner.
                ErrorMessage = "Vui lòng nhập mật khẩu.";
            }
            if (HasErrors || ErrorMessage != null)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var user = await _authService.LoginAsync(email, Password);
                if (user == null)
                {
                    ErrorMessage = "Email hoặc mật khẩu không đúng.";
                    return;
                }

                // Chi nho sau khi dang nhap THANH CONG, de khong luu lai mat khau sai.
                if (RememberMe)
                {
                    RememberedLogin.Save(email, Password);
                }
                else
                {
                    RememberedLogin.Clear();
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
