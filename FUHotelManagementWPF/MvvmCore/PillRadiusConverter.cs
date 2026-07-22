using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// Bo goc pill CHUAN cho WPF: tra ve nua chieu cao thuc te cua control.
    /// Ly do ton tai: WPF KHONG tu kep CornerRadius nhu CSS - dat 999 tren
    /// khoi chu nhat se ve thanh hinh ELIP meo (chu cham mep cong).
    /// Dung: CornerRadius="{Binding ActualHeight, RelativeSource={RelativeSource Self},
    ///        Converter={StaticResource PillRadius}}"
    /// </summary>
    public class PillRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is double height and > 0
                ? new CornerRadius(height / 2)
                : new CornerRadius(0);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
