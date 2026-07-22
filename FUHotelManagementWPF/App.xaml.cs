using System.Windows;
using Services;

namespace FUHotelManagementWPF;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Lan chay dau: tu tao database + seed du lieu mau, team khong phai setup gi
        // ngoai SQL Server Express. Migrate async nen splash van quay muot.
        var splash = new SplashWindow();
        splash.Show();

        try
        {
            await DatabaseService.EnsureMigratedAsync();
        }
        catch (Exception ex)
        {
            splash.Close();
            MessageBox.Show(
                "Không kết nối được SQL Server.\n\n" +
                "Kiểm tra: service SQL Server (SQLEXPRESS) đang chạy, " +
                "và chuỗi kết nối trong appsettings.json (hoặc appsettings.Local.json) đúng với máy bạn.\n\n" +
                "Chi tiết lỗi: " + ex.Message,
                "Lỗi khởi động",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
            return;
        }

        // Mo Login truoc roi moi dong splash de app khong tat (OnLastWindowClose)
        new LoginWindow().Show();
        splash.Close();
    }
}
