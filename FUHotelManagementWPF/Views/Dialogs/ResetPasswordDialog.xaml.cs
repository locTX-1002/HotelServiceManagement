using System.Windows;
using FUHotelManagementWPF.ViewModels.Users;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class ResetPasswordDialog : Window
{
    private readonly ResetPasswordDialogViewModel _viewModel;

    public ResetPasswordDialog(ResetPasswordDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }

    // Doc mat khau tai code-behind vi PasswordBox khong binding duoc; khong luu lai o dau.
    private async void Reset_Click(object sender, RoutedEventArgs e)
        => await _viewModel.ResetAsync(PasswordInput.Password, ConfirmInput.Password);
}
