using System.Windows;
using FUHotelManagementWPF.ViewModels.Invoices;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class InvoiceDetailDialog : Window
{
    public InvoiceDetailDialog(InvoicesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
