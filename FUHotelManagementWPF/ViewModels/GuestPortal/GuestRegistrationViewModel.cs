using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.GuestPortal;

/// <summary>Form tu dang ky cho khach vang lai tai man dang nhap.</summary>
public sealed class GuestRegistrationViewModel : ValidatableViewModelBase
{
    private readonly IGuestAccountService _accounts;

    private string _fullName = string.Empty;
    private string _phoneNumber = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;

    public GuestRegistrationViewModel() : this(new GuestAccountService()) { }

    public GuestRegistrationViewModel(IGuestAccountService accounts)
    {
        _accounts = accounts;
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, _ => !IsBusy);
    }

    public event Action<GuestAccount>? RegistrationSucceeded;

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public string PasswordHint => InputPolicy.PasswordHint;

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public AsyncRelayCommand RegisterCommand { get; }

    private async Task RegisterAsync(object? _)
    {
        ClearAllErrors();
        ErrorMessage = null;

        var fullName = FullName.Trim();
        var phone = NormalizePhone(PhoneNumber);
        var email = Email.Trim();

        if (string.IsNullOrWhiteSpace(fullName)) AddError(nameof(FullName), "Chưa nhập họ tên.");
        else if (fullName.Length > 100) AddError(nameof(FullName), "Họ tên tối đa 100 ký tự.");

        var phoneError = InputPolicy.ValidatePhone(phone, required: true);
        if (phoneError != null) AddError(nameof(PhoneNumber), phoneError);

        var emailError = InputPolicy.ValidateEmail(email, required: false);
        if (emailError != null) AddError(nameof(Email), emailError);

        var passwordError = PasswordPolicy.Validate(Password);
        if (passwordError != null) ErrorMessage = passwordError;
        else if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            ErrorMessage = "Mật khẩu xác nhận không khớp.";

        if (HasErrors || ErrorMessage != null)
        {
            ErrorMessage ??= FirstError();
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _accounts.RegisterAsync(
                fullName,
                phone,
                string.IsNullOrWhiteSpace(email) ? null : email,
                Password,
                ConfirmPassword);

            if (!result.Ok)
            {
                ErrorMessage = result.Message;
                return;
            }

            RegistrationSucceeded?.Invoke(result.Data!);
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể đăng ký lúc này. Kiểm tra kết nối cơ sở dữ liệu rồi thử lại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string NormalizePhone(string value)
        => (value ?? string.Empty).Trim()
            .Replace(" ", string.Empty)
            .Replace(".", string.Empty)
            .Replace("-", string.Empty);
}
