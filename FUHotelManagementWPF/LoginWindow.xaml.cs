using System.ComponentModel;
using System.Windows;
using FUHotelManagementWPF.ViewModels;

namespace FUHotelManagementWPF;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    /// <summary>Chan vong lap khi hai o mat khau day gia tri qua lai cho nhau.</summary>
    private bool _syncing;

    public LoginWindow()
    {
        InitializeComponent();

        _viewModel = new LoginViewModel();
        // Mo MainWindow TRUOC khi dong login de app khong tat
        // (ShutdownMode mac dinh la OnLastWindowClose).
        _viewModel.LoginSucceeded += () =>
        {
            new MainWindow().Show();
            Close();
        };
        // Khach tu dang nhap thi vao khu "Phong cua toi", khong vao man quan tri
        _viewModel.GuestLoginSucceeded += () =>
        {
            new GuestWindow().Show();
            Close();
        };
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        DataContext = _viewModel;

        // Dien san mat khau da nho tu lan truoc.
        PushToPasswordBox();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.Password))
        {
            PushToPasswordBox();
        }
    }

    /// <summary>Go trong PasswordBox thi day len ViewModel (o hien chu doc tu do).</summary>
    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        _syncing = true;
        _viewModel.Password = PasswordInput.Password;
        _syncing = false;
    }

    /// <summary>
    /// Chieu nguoc lai: ViewModel doi thi chep vao PasswordBox. Can vi PasswordBox
    /// khong binding duoc, nen mat khau nho san hoac go ben o hien chu se khong tu
    /// chay vao day.
    /// </summary>
    private void PushToPasswordBox()
    {
        if (_syncing || PasswordInput.Password == _viewModel.Password)
        {
            return;
        }
        _syncing = true;
        PasswordInput.Password = _viewModel.Password;
        _syncing = false;
    }
}
