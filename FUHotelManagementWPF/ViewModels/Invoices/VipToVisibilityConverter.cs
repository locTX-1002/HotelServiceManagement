using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using BusinessObjects.Enums;

namespace FUHotelManagementWPF.ViewModels.Invoices;

/// <summary>
/// Hien badge "VIP -10%" khi khach co nhan Vip. Dat rieng o module Hoa don thay vi
/// them vao Themes chung - chi mot cho dung, khong lam phinh tai nguyen toan app.
/// </summary>
public sealed class VipToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is GuestTag.Vip ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
