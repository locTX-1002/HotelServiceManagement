using System.Windows;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels;

namespace FUHotelManagementWPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new MainViewModel();
        // Mo LoginWindow truoc khi dong de app khong tat (OnLastWindowClose).
        viewModel.LoggedOut += () =>
        {
            new LoginWindow().Show();
            Close();
        };
        DataContext = viewModel;

        // Demo chuan feedback cua nhom: thao tac xong -> Notify.*, khong MessageBox
        Loaded += (_, _) => Notify.Success($"Xin chào {viewModel.GreetingName}!");
    }
}
