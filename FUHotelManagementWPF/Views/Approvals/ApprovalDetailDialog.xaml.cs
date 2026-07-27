using System.Windows;
using FUHotelManagementWPF.ViewModels.Approvals;
using FUHotelManagementWPF.ViewModels.Rooms;

namespace FUHotelManagementWPF.Views.Approvals;

public partial class ApprovalDetailDialog : Window
{
    private ApprovalDetailDialog(ApprovalRow row)
    {
        InitializeComponent();
        DataContext = row;
        Owner = RoomMapViewModel.ActiveWindow();
    }

    public static void ShowFor(ApprovalRow row)
    {
        _ = new ApprovalDetailDialog(row).ShowDialog();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
