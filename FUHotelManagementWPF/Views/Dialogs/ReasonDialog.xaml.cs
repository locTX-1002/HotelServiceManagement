using System.Windows;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class ReasonDialog : Window
{
    public string Reason => ReasonText.Text.Trim();

    public ReasonDialog(string title)
    {
        InitializeComponent();
        TitleText.Text = title;
        Loaded += (_, _) => ReasonText.Focus();
    }

    public static string? Prompt(string title, Window? owner)
    {
        var dialog = new ReasonDialog(title) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.Reason : null;
    }

    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ReasonText.Text))
        {
            ErrorText.Text = "Vui lòng nhập lý do.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }
        DialogResult = true;
    }
}
