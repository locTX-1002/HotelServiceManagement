using System.Windows;
using FUHotelManagementWPF.ViewModels.Users;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class ResetPasswordDialog : Window
{
    private readonly ResetPasswordDialogViewModel _viewModel;

    private bool _syncing;

    public ResetPasswordDialog(ResetPasswordDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        viewModel.RequestClose += ok => DialogResult = ok;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        DataContext = viewModel;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ResetPasswordDialogViewModel.IsPasswordVisible))
        {
            if (_syncing) return;
            _syncing = true;
            if (_viewModel.IsPasswordVisible)
            {
                PlainPasswordInput.Text = PasswordInput.Password;
                PlainConfirmInput.Text = ConfirmInput.Password;
            }
            else
            {
                PasswordInput.Password = PlainPasswordInput.Text;
                ConfirmInput.Password = PlainConfirmInput.Text;
            }
            _syncing = false;
        }
    }

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        _syncing = true;
        PlainPasswordInput.Text = PasswordInput.Password;
        _syncing = false;
    }

    private void PlainPasswordInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_syncing) return;
        _syncing = true;
        PasswordInput.Password = PlainPasswordInput.Text;
        _syncing = false;
    }

    private void ConfirmInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        _syncing = true;
        PlainConfirmInput.Text = ConfirmInput.Password;
        _syncing = false;
    }

    private void PlainConfirmInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_syncing) return;
        _syncing = true;
        ConfirmInput.Password = PlainConfirmInput.Text;
        _syncing = false;
    }

    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        var password = _viewModel.IsPasswordVisible ? PlainPasswordInput.Text : PasswordInput.Password;
        var confirm = _viewModel.IsPasswordVisible ? PlainConfirmInput.Text : ConfirmInput.Password;

        try
        {
            await _viewModel.ResetAsync(password, confirm);
        }
        catch (System.Exception ex)
        {
            MvvmCore.Notify.Error($"Lỗi: {ex.Message}");
        }
    }
}
