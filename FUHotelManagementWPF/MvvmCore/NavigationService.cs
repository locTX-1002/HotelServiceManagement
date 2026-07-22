using System;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// Dieu huong ViewModel-first dung chung: module can nhay sang module khac thi goi
    /// NavigationService.NavigateTo("Hoá đơn") - khong tu new UserControl, khong dung Frame.
    /// Dat o tang WPF (khong phai project Services) vi dieu huong la viec cua UI;
    /// Services la class library thuan nghiep vu, khong duoc biet ViewModel/View.
    /// MainViewModel dang ky ham xu ly qua Configure() luc khoi tao.
    /// </summary>
    public static class NavigationService
    {
        private static Action<string>? _navigate;

        public static void Configure(Action<string> navigate) => _navigate = navigate;

        /// <summary>Chuyen sang module theo dung ten hien thi tren sidebar.</summary>
        public static void NavigateTo(string moduleTitle) => _navigate?.Invoke(moduleTitle);
    }
}
