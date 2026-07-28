using System.Windows;
using FUHotelManagementWPF.ViewModels.Users;

namespace FUHotelManagementWPF.Views.Dialogs
{
    public partial class CreateGuestAccountDialog : Window
    {
        private readonly CreateGuestAccountDialogViewModel _vm;

        public CreateGuestAccountDialog()
        {
            InitializeComponent();
            _vm = new CreateGuestAccountDialogViewModel();
            DataContext = _vm;

            // Đóng window khi lưu thành công
            _vm.RequestClose += success =>
            {
                DialogResult = success;
                Close();
            };
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Truyền mật khẩu từ PasswordBox vào ViewModel
            await _vm.SaveAsync(PasswordInput.Password, ConfirmInput.Password);
        }
    }
}