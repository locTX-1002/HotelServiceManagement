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

        /// <summary>
        /// Thu can ban giao khi nhay man. Vi du: check-out bi chan vi chua thanh toan
        /// thi nhay sang Hoa don VA chon san dung luot do, le tan khong phai tu do tim.
        ///
        /// De o day thay vi truyen qua NavigateTo vi module dich duoc tao MOI luc nhay:
        /// ViewModel dich doc gia tri nay trong constructor cua no.
        /// </summary>
        public static int? PendingStayId { get; set; }

        /// <summary>Lay ra roi xoa luon - tranh lan sau vao man lai bi chon nham don cu.</summary>
        public static int? TakePendingStayId()
        {
            var value = PendingStayId;
            PendingStayId = null;
            return value;
        }
    }
}
