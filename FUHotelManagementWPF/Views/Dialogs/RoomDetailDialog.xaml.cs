using System.Windows;
using FUHotelManagementWPF.ViewModels.Rooms;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class RoomDetailDialog : Window
{
    public RoomDetailDialog(RoomDetailDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
