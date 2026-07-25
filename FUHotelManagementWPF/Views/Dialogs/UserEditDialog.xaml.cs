using System.Windows;
using FUHotelManagementWPF.ViewModels.Users;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class UserEditDialog : Window
{
    private readonly UserEditDialogViewModel _viewModel;

    public UserEditDialog(UserEditDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }

    // PasswordBox khong binding duoc (bao mat cua WPF) nen doc thang tai day roi truyen
    // vao ViewModel - giong ActivateAccountDialog. Mat khau khong nam trong property nao.
    private async void Save_Click(object sender, RoutedEventArgs e)
        => await _viewModel.SaveAsync(PasswordInput.Password, ConfirmInput.Password);
}
