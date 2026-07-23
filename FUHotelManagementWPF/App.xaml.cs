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
        catch (InvalidOperationException ex)
        {
            // Loi cau hinh, khong phai loi SQL - noi dung viec can lam thay vi do tai SQL Server
            splash.Close();
            MessageBox.Show(
                "Thiếu cấu hình để khởi động.\n\n" + ex.Message + "\n\n" +
                "Tạo file FUHotelManagementWPF/appsettings.Local.json (file này đã gitignore, " +
                "mỗi máy một bản riêng) với nội dung:\n\n" +
                "{\n" +
                "  \"BootstrapAdmin\": {\n" +
                "    \"Email\": \"admin@hotel.com\",\n" +
                "    \"FullName\": \"Hotel Administrator\",\n" +
                "    \"Password\": \"MatKhauCuaBan@2026\"\n" +
                "  }\n" +
                "}\n\n" +
                "Mật khẩu phải từ 8 ký tự, có chữ hoa, chữ thường, chữ số và ký tự đặc biệt.",
                "Lỗi khởi động",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown(-1);
            return;
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
