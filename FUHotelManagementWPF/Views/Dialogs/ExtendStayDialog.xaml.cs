using System.Windows;
using FUHotelManagementWPF.ViewModels.CheckInOut;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class ExtendStayDialog : Window
{
    public ExtendStayDialog(ExtendStayDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += ok =>
        {
            DialogResult = ok;
            Close();
        };
    }
}
