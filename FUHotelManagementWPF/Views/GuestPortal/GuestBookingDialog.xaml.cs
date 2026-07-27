using System.Windows;
using FUHotelManagementWPF.ViewModels.GuestPortal;

namespace FUHotelManagementWPF.Views.GuestPortal;

public partial class GuestBookingDialog : Window
{
    public GuestBookingDialog(GuestBookingDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }
}
