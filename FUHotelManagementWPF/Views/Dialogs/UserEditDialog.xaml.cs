using System.Windows;
using FUHotelManagementWPF.ViewModels.Users;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class UserEditDialog : Window
{
    private readonly UserEditDialogViewModel _viewModel;

    private bool _syncing;

    public UserEditDialog(UserEditDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        viewModel.RequestClose += ok => DialogResult = ok;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        DataContext = viewModel;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserEditDialogViewModel.IsPasswordVisible))
        {
            if (_syncing) return;
            _syncing = true;
            if (_viewModel.IsPasswordVisible && PasswordInput != null && PlainPasswordInput != null)
            {
                PlainPasswordInput.Text = PasswordInput.Password;
                PlainConfirmInput.Text = ConfirmInput.Password;
            }
            else if (!_viewModel.IsPasswordVisible && PasswordInput != null && PlainPasswordInput != null)
            {
                PasswordInput.Password = PlainPasswordInput.Text;
                ConfirmInput.Password = PlainConfirmInput.Text;
            }
            _syncing = false;
        }
    }

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing || PlainPasswordInput == null) return;
        _syncing = true;
        PlainPasswordInput.Text = PasswordInput.Password;
        _syncing = false;
    }

    private void PlainPasswordInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_syncing || PasswordInput == null) return;
        _syncing = true;
        PasswordInput.Password = PlainPasswordInput.Text;
        _syncing = false;
    }

    private void ConfirmInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing || PlainConfirmInput == null) return;
        _syncing = true;
        PlainConfirmInput.Text = ConfirmInput.Password;
        _syncing = false;
    }

    private void PlainConfirmInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_syncing || ConfirmInput == null) return;
        _syncing = true;
        ConfirmInput.Password = PlainConfirmInput.Text;
        _syncing = false;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var password = (_viewModel.IsCreate && _viewModel.IsPasswordVisible && PlainPasswordInput != null)
            ? PlainPasswordInput.Text
            : (PasswordInput != null ? PasswordInput.Password : string.Empty);

        var confirm = (_viewModel.IsCreate && _viewModel.IsPasswordVisible && PlainConfirmInput != null)
            ? PlainConfirmInput.Text
            : (ConfirmInput != null ? ConfirmInput.Password : string.Empty);

        try
        {
            await _viewModel.SaveAsync(password, confirm);
        }
        catch (System.Exception ex)
        {
            MvvmCore.Notify.Error($"Lỗi: {ex.Message}");
        }
    }
}
