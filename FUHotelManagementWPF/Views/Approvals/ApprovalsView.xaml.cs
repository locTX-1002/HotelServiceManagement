using System.Windows.Controls;
using BusinessObjects.Enums;
using FUHotelManagementWPF.ViewModels.Approvals;

namespace FUHotelManagementWPF.Views.Approvals;

public partial class ApprovalsView : UserControl
{
    public ApprovalsView() => InitializeComponent();

    private void RequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ApprovalsViewModel vm
            || vm.SelectedStatus == ApprovalRequestStatus.Pending
            || e.AddedItems.Count == 0
            || e.AddedItems[0] is not ApprovalRow row)
        {
            return;
        }

        // Lich su chi mo chi tiet khi nguoi dung click vao dong. Khong dat cac thong tin
        // nguoi duyet/ghi chu o day de man danh sach gon va tranh nham voi action Pending.
        ApprovalDetailDialog.ShowFor(row);

        // Bo chon sau khi dong dialog de co the click lai cung mot dong va mo chi tiet lan nua.
        RequestsGrid.SelectedItem = null;
        vm.SelectedRequest = null;
    }
}
