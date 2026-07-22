using System.Windows;
using FUHotelManagementWPF.ViewModels.Rooms;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class RoomEditDialog : Window
{
    public RoomEditDialog(RoomEditDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }
}
