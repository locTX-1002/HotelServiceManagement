using FUHotelManagementWPF.MvvmCore;

namespace FUHotelManagementWPF.ViewModels
{
    /// <summary>Noi dung tam cho module chua lam - thanh vien lam xong thi thay bang ViewModel that.</summary>
    public class PlaceholderViewModel : ViewModelBase
    {
        public string Title { get; }

        public PlaceholderViewModel(string title) => Title = title;
    }
}
