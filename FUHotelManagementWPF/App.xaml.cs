using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using Services;

namespace FUHotelManagementWPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ForceVietnamese();
        base.OnStartup(e);
        _ = StartupAsync();
    }

    /// <summary>
    /// Ep tieng Viet cho toan app. Lich cua DatePicker mac dinh bi HandyControl
    /// (thu vien Trung Quoc) doi sang tieng Trung; may deu la en-US nhung app van
    /// hien 年/月. Dat truoc khi mo cua so dau tien nen khong con cho nao lo tieng.
    ///
    /// Language cua WPF (xml:lang) moi la thu quyet dinh ten thu/thang tren lich,
    /// khong phai CurrentCulture - nen phai override rieng.
    /// </summary>
    private static void ForceVietnamese()
    {
        var vi = new CultureInfo("vi-VN");
        Thread.CurrentThread.CurrentCulture = vi;
        Thread.CurrentThread.CurrentUICulture = vi;
        CultureInfo.DefaultThreadCurrentCulture = vi;
        CultureInfo.DefaultThreadCurrentUICulture = vi;
        try
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("vi-VN")));
        }
        catch (Exception)
        {
            // Neu thu vien khac da override roi thi bo qua - style DatePicker o duoi
            // van dat Language="vi-VN" tren tung o nen lich van ra tieng Viet.
        }
    }

    private async System.Threading.Tasks.Task StartupAsync()
    {

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
