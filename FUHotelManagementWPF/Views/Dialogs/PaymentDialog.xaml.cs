using System.Windows;
using FUHotelManagementWPF.ViewModels.Invoices;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class PaymentDialog : Window
{
    public PaymentDialog(PaymentDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }
}
