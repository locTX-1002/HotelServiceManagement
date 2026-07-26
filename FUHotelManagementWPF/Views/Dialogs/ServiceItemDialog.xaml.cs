using System.Windows;
using FUHotelManagementWPF.ViewModels.Services;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class ServiceItemDialog : Window
{
    public ServiceItemDialog(ServiceItemDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }
}
