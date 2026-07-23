using System.Windows;
using FUHotelManagementWPF.ViewModels.Reservations;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class CreateReservationDialog : Window
{
    public CreateReservationDialog(CreateReservationDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }
}
