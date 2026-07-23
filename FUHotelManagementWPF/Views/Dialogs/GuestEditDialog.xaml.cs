using System.Windows;
using FUHotelManagementWPF.ViewModels.Guests;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class GuestEditDialog : Window
{
    public GuestEditDialog(GuestEditDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }
}
