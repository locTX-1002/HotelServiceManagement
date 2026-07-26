using System;
using System.Globalization;
using System.Windows.Data;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// Hien so tien theo kieu Viet Nam: "1.250.000 đ".
    ///
    /// Truoc day moi cho tu viet <c>StringFormat={}{0:N0}</c>, ma StringFormat lay dinh dang
    /// tu <c>FrameworkElement.Language</c> cua chinh phan tu do. Phan tu nam trong popup
    /// (danh sach xo xuong cua ComboBox, ToolTip, ContextMenu) thi o mot cay truc quan khac
    /// nen KHONG thua ke Language cua app - ra "1,250,000" dau phay trong khi phan con lai
    /// cua man hinh la "1.250.000". Converter nay chi dinh thang vi-VN nen dat o dau cung
    /// ra mot kieu.
    /// </summary>
    public sealed class MoneyConverter : IValueConverter
    {
        private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

        /// <summary>Truyen ConverterParameter="raw" khi chi muon con so, khong kem chu "đ".</summary>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            decimal amount;
            try
            {
                amount = System.Convert.ToDecimal(value, Vietnamese);
            }
            catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
            {
                return value.ToString() ?? string.Empty;
            }

            var text = amount.ToString("N0", Vietnamese);
            return parameter as string == "raw" ? text : $"{text} đ";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
