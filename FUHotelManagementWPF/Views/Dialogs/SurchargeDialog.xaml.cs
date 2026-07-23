using System.Windows;
using FUHotelManagementWPF.ViewModels.CheckInOut;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class SurchargeDialog : Window
{
    public SurchargeDialog(SurchargeDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
