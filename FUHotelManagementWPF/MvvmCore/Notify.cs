using HandyControl.Controls;

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
        public static void Success(string message) => Growl.Success(message);
        public static void Info(string message) => Growl.Info(message);
        public static void Warning(string message) => Growl.Warning(message);
        public static void Error(string message) => Growl.Error(message);
    }
}
