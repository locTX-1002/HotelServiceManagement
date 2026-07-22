using System.Windows;
using FUHotelManagementWPF.ViewModels.Rooms;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class RoomStatusDialog : Window
{
    public RoomStatusDialog(RoomStatusDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }
}
