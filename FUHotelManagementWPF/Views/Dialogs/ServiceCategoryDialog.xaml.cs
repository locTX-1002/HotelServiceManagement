using System.Windows;
using FUHotelManagementWPF.ViewModels.Services;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class ServiceCategoryDialog : Window
{
    public ServiceCategoryDialog(ServiceCategoryDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }
}
