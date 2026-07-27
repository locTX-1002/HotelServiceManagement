using System;
using HandyControl.Controls;
using HandyControl.Data;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// Thong bao nhe goc man hinh (Growl cua HandyControl) - QUY UOC NHOM: moi feedback
    /// CRUD (luu xong, xoa xong, loi mang...) dung Notify.*, KHONG dung MessageBox.
    /// MessageBox chi danh cho loi chan app luc khoi dong va dialog xac nhan xoa.
    /// Panel nhan thong bao dat san trong MainWindow (hc:Growl.GrowlParent="True").
    /// </summary>
    public static class Notify
    {
        /// <summary>
        /// Bam mot nut hai lan lien tiep thi ra hai thong bao y het nhau, chong len nhau che
        /// mat noi dung ben duoi. Bo qua neu vua bao dung cau nay trong vong hai giay.
        /// </summary>
        private static readonly TimeSpan RepeatWindow = TimeSpan.FromSeconds(2);
        private static string _lastMessage = string.Empty;
        private static DateTime _lastShownAt = DateTime.MinValue;

        private static bool IsRepeat(string message)
        {
            var now = DateTime.Now;
            if (message == _lastMessage && now - _lastShownAt < RepeatWindow)
            {
                return true;
            }
            _lastMessage = message;
            _lastShownAt = now;
            return false;
        }

        /// <summary>Bao thanh cong tu tat sau 3 giay - khong bat nguoi dung bam dong.</summary>
        private static void Show(string message, Action<GrowlInfo> show, int waitSeconds)
        {
            if (IsRepeat(message))
            {
                return;
            }

            void ExecuteShow()
            {
                try
                {
                    show(new GrowlInfo
                    {
                        Message = message,
                        WaitTime = waitSeconds,
                        ShowDateTime = true,
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Notify Error] {ex.Message}");
                }
            }

            var app = System.Windows.Application.Current;
            if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.BeginInvoke((Action)ExecuteShow);
            }
            else
            {
                ExecuteShow();
            }
        }

        public static void Success(string message) => Show(message, Growl.Success, 3);
        public static void Info(string message) => Show(message, Growl.Info, 3);
        public static void Warning(string message) => Show(message, Growl.Warning, 5);

        // Loi thi de lau hon, va van cho hien lai neu nguoi dung thu lai roi hong tiep.
        public static void Error(string message) => Show(message, Growl.Error, 8);
    }
}
