using System.Windows;
using FUHotelManagementWPF.ViewModels;

namespace FUHotelManagementWPF;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        var viewModel = new LoginViewModel();
        // Mo MainWindow TRUOC khi dong login de app khong tat
        // (ShutdownMode mac dinh la OnLastWindowClose).
        viewModel.LoginSucceeded += () =>
        {
            new MainWindow().Show();
            Close();
        };
        DataContext = viewModel;
    }
}
