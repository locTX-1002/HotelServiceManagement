using System.Windows;
using FUHotelManagementWPF.ViewModels.Promotions;

namespace FUHotelManagementWPF.Views.Dialogs;

public partial class PromotionEditDialog : Window
{
    public PromotionEditDialog(PromotionEditDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.RequestClose += ok => DialogResult = ok;
        DataContext = viewModel;
    }
}
