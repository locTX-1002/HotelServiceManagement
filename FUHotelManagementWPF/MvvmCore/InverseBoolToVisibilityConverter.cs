using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// Nguoc lai voi BoolToVis: true thi AN, false thi hien.
    /// Dung cho cac cap "co du lieu / chua co du lieu" - vi du gio hang rong thi hien
    /// dong huong dan, co mon roi thi hien bang tong tien. Truoc day phai them mot
    /// property phu kieu IsNotEmpty trong ViewModel chi de dao mot gia tri bool.
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility.Collapsed;
    }
}
