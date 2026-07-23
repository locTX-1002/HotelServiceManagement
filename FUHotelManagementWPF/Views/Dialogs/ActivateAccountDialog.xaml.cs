using System.Windows;
using FUHotelManagementWPF.ViewModels.Guests;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class ActivateAccountDialog : Window
{
    private readonly ActivateAccountDialogViewModel _viewModel;

    public ActivateAccountDialog(ActivateAccountDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }

    // PasswordBox khong binding duoc (bao mat cua WPF) nen doc tai day roi dua thang vao VM,
    // giong cach LoginWindow lam - mat khau khong nam trong property/binding nao.
    private async void Activate_Click(object sender, RoutedEventArgs e)
        => await _viewModel.ActivateAsync(PasswordInput.Password, ConfirmInput.Password);
}
