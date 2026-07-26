using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// Tu cham dau phan nghin NGAY TRONG LUC GO cho o nhap tien: "10000000" thanh
    /// "10.000.000". Nhin day so tran khong ai dem duoc, go nham mot chu so la lech gap
    /// muoi lan ma khong ai nhan ra.
    ///
    /// Chi cho go chu so; moi ky tu khac bi bo qua. Con tro duoc dat lai theo SO CHU SO
    /// dung truoc no chu khong theo vi tri ky tu - khong thi cham vua chen vao se day con
    /// tro nhay lung tung moi lan qua moc nghin.
    ///
    /// ViewModel van nhan chuoi co dau cham; dung <see cref="ToDecimal"/> de doc ra so.
    /// </summary>
    public static class MoneyBoxAssist
    {
        private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable", typeof(bool), typeof(MoneyBoxAssist),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);

        /// <summary>Doc so tien tu chuoi da cham: bo het ky tu khong phai chu so.</summary>
        public static decimal? ToDecimal(string? text)
        {
            var digits = new string((text ?? string.Empty).Where(char.IsDigit).ToArray());
            return digits.Length == 0 ? null
                : decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value
                : null;
        }

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox box)
            {
                return;
            }

            box.PreviewTextInput -= OnTextInput;
            box.TextChanged -= OnTextChanged;
            DataObject.RemovePastingHandler(box, OnPaste);

            if (e.NewValue is true)
            {
                box.PreviewTextInput += OnTextInput;
                box.TextChanged += OnTextChanged;
                DataObject.AddPastingHandler(box, OnPaste);
            }
        }

        // Chan luon ky tu khong phai chu so ngay tu ban phim, khoi phai don o buoc sau.
        private static void OnTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !e.Text.All(char.IsDigit);

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var pasted = e.DataObject.GetData(DataFormats.UnicodeText) as string;
            if (pasted == null || !pasted.Any(char.IsDigit))
            {
                e.CancelCommand();
            }
        }

        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox box)
            {
                return;
            }

            var raw = box.Text;
            var digits = new string(raw.Where(char.IsDigit).ToArray()).TrimStart('0');
            if (digits.Length == 0)
            {
                // Go het thi de trong han, khong hien "0" lam nguoi dung phai xoa them.
                if (raw.Length > 0 && !raw.Any(char.IsDigit)) { Set(box, string.Empty, 0); }
                return;
            }

            var formatted = decimal.Parse(digits, CultureInfo.InvariantCulture).ToString("N0", Vietnamese);
            if (formatted == raw)
            {
                return;
            }

            // Dem so chu so nam truoc con tro de dat lai cho dung sau khi chen dau cham.
            var digitsBeforeCaret = raw.Take(box.SelectionStart).Count(char.IsDigit);
            var caret = 0;
            var seen = 0;
            while (caret < formatted.Length && seen < digitsBeforeCaret)
            {
                if (char.IsDigit(formatted[caret])) { seen++; }
                caret++;
            }
            Set(box, formatted, caret);
        }

        private static void Set(TextBox box, string text, int caret)
        {
            box.TextChanged -= OnTextChanged;
            box.Text = text;
            box.SelectionStart = System.Math.Clamp(caret, 0, text.Length);
            box.SelectionLength = 0;
            box.TextChanged += OnTextChanged;
        }
    }
}
