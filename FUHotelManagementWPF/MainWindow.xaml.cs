using System.Windows;
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
    }
}
