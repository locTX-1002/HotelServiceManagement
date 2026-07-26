using System.Windows;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels.GuestPortal;

namespace FUHotelManagementWPF;

/// <summary>Khu danh cho KHACH tu dang nhap - tach hoan toan khoi MainWindow cua nhan vien.</summary>
public partial class GuestWindow : Window
{
    public GuestWindow()
    {
        InitializeComponent();

        var viewModel = new GuestShellViewModel();
        // Mo LoginWindow truoc khi dong de app khong tat (ShutdownMode OnLastWindowClose).
        viewModel.LoggedOut += () =>
        {
            new LoginWindow().Show();
            Close();
        };
        DataContext = viewModel;

        Loaded += (_, _) => Notify.Success($"Xin chào {viewModel.GuestName}!");
    }
}
