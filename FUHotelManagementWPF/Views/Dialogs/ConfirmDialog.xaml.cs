using System.Windows;
using System.Windows.Media;

namespace FUHotelManagementWPF.Views.Dialogs;

/// <summary>
/// Hop thoai xac nhan dung chung cho ca app, thay cho MessageBox mac dinh cua Windows
/// (font khac, mau khac, nut Yes/No tieng Anh - nhin nhu cua ung dung khac lot vao).
///
/// Dung: if (ConfirmDialog.Ask("Xoa phong 101?", "Phong da co lich su dat...")) { ... }
/// </summary>
public partial class ConfirmDialog : Window
{
    private ConfirmDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Hoi mot cau co/khong. Tra ve true khi nguoi dung bam nut xac nhan.
    /// </summary>
    /// <param name="title">Cau hoi ngan, doc la hieu ngay dang lam gi.</param>
    /// <param name="message">Hau qua cua viec sap lam, viet cho nguoi khong biet ky thuat.</param>
    /// <param name="note">Luu y phu, hien trong khung nen nhat. Bo trong thi khong hien.</param>
    /// <param name="confirmLabel">Chu tren nut chinh - dat theo dung viec, dung "OK".</param>
    /// <param name="isDanger">Viec khong lui lai duoc thi de true: dai mau va nut chuyen do.</param>
    public static bool Ask(string title, string message, string? note = null,
        string confirmLabel = "Xác nhận", bool isDanger = false)
    {
        var dialog = new ConfirmDialog
        {
            Owner = ViewModels.Rooms.RoomMapViewModel.ActiveWindow(),
        };
        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;
        dialog.OkButton.Content = confirmLabel;

        if (!string.IsNullOrWhiteSpace(note))
        {
            dialog.NoteText.Text = note;
            dialog.NoteBox.Visibility = Visibility.Visible;
        }

        if (isDanger)
        {
            var danger = (Brush)Application.Current.Resources["DangerBrush"];
            dialog.AccentBar.Background = danger;
            dialog.OkButton.Background = danger;
        }

        return dialog.ShowDialog() == true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
