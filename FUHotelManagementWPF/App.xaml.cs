using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using HandyControl.Tools;
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
        WriteCrashLog(fullDetail);

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

    /// <summary>
    /// Ghi loi ra file loi.log NAM CANH FILE CHAY. Khong dung duong dan tuyet doi cua
    /// mot may nao ca - moi nguoi trong nhom chay tren o dia khac nhau.
    /// Ghi log ma hong thi bo qua: dang xu ly su co roi, khong duoc de no de ra su co moi.
    /// </summary>
    private static void WriteCrashLog(string detail)
    {
        try
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "loi.log");
            System.IO.File.AppendAllText(path, $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {detail}\n\n----------\n\n");
        }
        catch (Exception)
        {
            // Het cho ghi / khong co quyen ghi - chiu, van de app chay tiep.
        }
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Unhandled Domain Exception] {ex}");
            WriteCrashLog(ex.ToString());
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[Unobserved Task Exception] {e.Exception}");
        e.SetObserved();
    }

    /// <summary>
    /// Ep tieng Viet cho toan app. Rieng lich cua DatePicker khong doi duoc: no dung
    /// HandyControl (thu vien Trung Quoc), va HandyControl co he thong ngon ngu RIENG
    /// cua no (ConfigHelper.SetLang), khong doc Thread.CurrentCulture hay
    /// FrameworkElement.Language cua WPF - nen 2 dong o duoi khong anh huong gi den
    /// lich ca, chi doi duoc cac control WPF thuan.
    ///
    /// HandyControl khong co goi ngon ngu tieng Viet (chi co en/fr/es/ru/ja/ko/pl/cs...),
    /// nen chon "en" - it nhat khong con la tieng Trung. Phai cai them goi NuGet rieng
    /// HandyControl.Lang.en thi SetLang("en") moi co file de doc, chi goi ham la chua du.
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
            // Neu thu vien khac da override roi thi bo qua - khong lien quan gi den
            // lich HandyControl, chi anh huong cac control WPF thuan khac.
        }

        ConfigHelper.Instance.SetLang("en");
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
