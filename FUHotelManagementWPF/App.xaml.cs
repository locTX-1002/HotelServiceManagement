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
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        ForceVietnamese();
        base.OnStartup(e);
        _ = StartupAsync();
    }

    private static bool _isDisplayingExceptionDialog;
    private static string _lastExceptionMessage = string.Empty;
    private static DateTime _lastExceptionTime = DateTime.MinValue;

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var fullDetail = e.Exception.ToString();
        System.Diagnostics.Debug.WriteLine($"[Unhandled UI Exception] {fullDetail}");

        try
        {
            var logPath = @"C:\Users\qhung\.gemini\antigravity-ide\brain\fcfacae3-b246-476f-9b4b-566807a98a82\crash_debug.log";
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] {fullDetail}\n\n--------------------\n\n");
        }
        catch { }

        e.Handled = true;

        var now = DateTime.Now;
        if (_isDisplayingExceptionDialog || (e.Exception.Message == _lastExceptionMessage && (now - _lastExceptionTime).TotalSeconds < 3))
        {
            return;
        }

        _lastExceptionMessage = e.Exception.Message;
        _lastExceptionTime = now;
        _isDisplayingExceptionDialog = true;
        try
        {
            var innerMsg = e.Exception.InnerException?.Message;
            var displayMsg = string.IsNullOrEmpty(innerMsg) ? e.Exception.Message : $"{e.Exception.Message}\n({innerMsg})";
            MessageBox.Show(
                $"Đã xảy ra lỗi hệ thống không mong muốn:\n\n{displayMsg}\n\nỨng dụng sẽ tiếp tục chạy.",
                "Lỗi ứng dụng",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _isDisplayingExceptionDialog = false;
        }
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Unhandled Domain Exception] {ex}");
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[Unobserved Task Exception] {e.Exception}");
        e.SetObserved();
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
