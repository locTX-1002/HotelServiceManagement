using System.Windows;
using FUHotelManagementWPF.ViewModels.Invoices;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class SurchargeEditDialog : Window
{
    public SurchargeEditDialog(SurchargeEditDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }
}
