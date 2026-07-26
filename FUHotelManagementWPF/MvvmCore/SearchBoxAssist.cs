using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace FUHotelManagementWPF.MvvmCore
{
    /// <summary>
    /// Gan hanh vi ban phim cho o tim kiem.
    ///
    /// Danh sach da loc ngay trong luc go (binding co Delay 250-300ms), nen mot nut "Tìm"
    /// rieng se khong lam gi them. Cai thuc su thieu la:
    ///   - Enter: day chu vao binding NGAY, khong doi het Delay. Nguoi dung go xong bam
    ///     Enter theo phan xa, khong thay gi phan hoi thi tuong o tim khong an.
    ///   - Escape / nut X: xoa nhanh de quay ve danh sach day du, thay vi phai xoa tay
    ///     tung ky tu.
    ///
    /// Dat <c>mvvm:SearchBoxAssist.Enable="True"</c> tren TextBox (da dat san trong style
    /// SearchBox nen moi o tim kiem trong app tu co).
    /// </summary>
    public static class SearchBoxAssist
    {
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable", typeof(bool), typeof(SearchBoxAssist),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox box)
            {
                return;
            }

            box.PreviewKeyDown -= OnKeyDown;
            box.Loaded -= OnLoaded;

            if (e.NewValue is true)
            {
                box.PreviewKeyDown += OnKeyDown;
                box.Loaded += OnLoaded;
            }
        }

        /// <summary>
        /// Gan nut "Tìm" ben canh mot o tim kiem: bam la day chu vao binding ngay, khong
        /// doi het Delay. Dat tren Button, tro toi TextBox bang ElementName.
        ///
        /// Danh sach von da loc trong luc go nen nut nay khong doi hanh vi may - no la cho
        /// bam quen tay, va la dau hieu nhin phat biet o do de tim.
        /// </summary>
        public static readonly DependencyProperty SubmitForProperty =
            DependencyProperty.RegisterAttached(
                "SubmitFor", typeof(TextBox), typeof(SearchBoxAssist),
                new PropertyMetadata(null, OnSubmitForChanged));

        public static TextBox? GetSubmitFor(DependencyObject element) => (TextBox?)element.GetValue(SubmitForProperty);
        public static void SetSubmitFor(DependencyObject element, TextBox? value) => element.SetValue(SubmitForProperty, value);

        private static void OnSubmitForChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Button button)
            {
                return;
            }

            button.Click -= OnSubmitClick;
            if (e.NewValue is TextBox)
            {
                button.Click += OnSubmitClick;
            }
        }

        private static void OnSubmitClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && GetSubmitFor(button) is { } box)
            {
                box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                box.Focus();
                box.CaretIndex = box.Text.Length;
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox box && box.Template.FindName("PART_Clear", box) is Button clear)
            {
                clear.Click -= OnClearClick;
                clear.Click += OnClearClick;
            }
        }

        private static void OnClearClick(object sender, RoutedEventArgs e)
        {
            // Nut X nam trong template nen cha cua no la chinh o tim kiem.
            if (sender is Button { TemplatedParent: TextBox box })
            {
                Clear(box);
            }
        }

        private static void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox box)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                    box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                    e.Handled = true;
                    break;
                case Key.Escape when box.Text.Length > 0:
                    Clear(box);
                    e.Handled = true;
                    break;
            }
        }

        private static void Clear(TextBox box)
        {
            box.Clear();
            // Binding co Delay nen phai day tay, khong thi danh sach loc cho them 300ms.
            box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            box.Focus();
        }
    }
}
