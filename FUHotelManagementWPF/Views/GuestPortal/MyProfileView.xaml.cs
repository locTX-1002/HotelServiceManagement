using System.Windows;
using System.Windows.Controls;
using FUHotelManagementWPF.ViewModels.GuestPortal;

namespace FUHotelManagementWPF.Views.GuestPortal;

public partial class MyProfileView : UserControl
{
    private MyProfileViewModel? _viewModel;

    public MyProfileView()
    {
        InitializeComponent();

        // View nay do ContentControl tao ra roi moi gan DataContext, nen phai bat o day
        // moi biet ViewModel de dang ky su kien xoa trang o mat khau.
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PasswordAccepted -= ClearPasswordBoxes;
        }

        _viewModel = e.NewValue as MyProfileViewModel;

        if (_viewModel != null)
        {
            _viewModel.PasswordAccepted += ClearPasswordBoxes;
        }
    }

    // Doc mat khau tai code-behind vi PasswordBox khong binding duoc (bao mat cua WPF);
    // ba chuoi di thang vao tham so, khong luu lai o dau. ViewModel tu chan bam doi
    // bang co IsBusy nen khong can AsyncRelayCommand o day.
    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        await _viewModel.ChangePasswordAsync(
            CurrentInput.Password, NewInput.Password, ConfirmInput.Password);
    }

    private void ClearPasswordBoxes()
    {
        CurrentInput.Clear();
        NewInput.Clear();
        ConfirmInput.Clear();
    }
}
