using System.Windows;
using FUHotelManagementWPF.ViewModels.Rooms;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class RoomTypeEditDialog : Window
{
    public RoomTypeEditDialog(RoomTypeEditDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }
}
