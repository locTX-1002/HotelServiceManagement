using System.Windows;
using BusinessObjects.Entities;
using FUHotelManagementWPF.ViewModels.GuestPortal;

namespace FUHotelManagementWPF;

public partial class RegisterGuestWindow : Window
{
    private readonly GuestRegistrationViewModel _viewModel;
    private bool _completed;

    public RegisterGuestWindow()
    {
        InitializeComponent();
        _viewModel = new GuestRegistrationViewModel();
        _viewModel.RegistrationSucceeded += OnRegistrationSucceeded;
        DataContext = _viewModel;
    }

    public GuestAccount? RegisteredAccount { get; private set; }

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        => _viewModel.Password = PasswordInput.Password;

    private void ConfirmPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        => _viewModel.ConfirmPassword = ConfirmPasswordInput.Password;

    private void OnRegistrationSucceeded(GuestAccount account)
    {
        RegisteredAccount = account;
        _completed = true;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (!_completed) DialogResult = false;
    }
}
