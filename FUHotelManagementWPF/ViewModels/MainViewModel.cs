using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels
{
    /// <summary>Mot muc dieu huong: icon Segoe MDL2 + ten module + nhom sidebar + ham tao ViewModel.</summary>
    public record ModuleItem(string Icon, string Title, string Group, Func<ViewModelBase> CreateViewModel);

    public class MainViewModel : ViewModelBase
    {
        /// <summary>View lang nghe de quay ve LoginWindow.</summary>
        public event Action? LoggedOut;

        public string GreetingName => AppSession.CurrentUser?.FullName ?? string.Empty;

        public string AvatarInitial
            => string.IsNullOrWhiteSpace(GreetingName) ? "?" : GreetingName.Trim()[..1].ToUpper();

        public string RoleDisplay => AppSession.RoleName switch
        {
            "Admin" => "Quản trị viên",
            "Manager" => "Quản lý",
            "Receptionist" => "Lễ tân",
            "ServiceStaff" => "Nhân viên dịch vụ",
            _ => AppSession.RoleName,
        };

        // Danh sach module: thanh vien lam xong module nao thi doi factory cua module do
        // sang ViewModel that va them 1 dong DataTemplate vao Views/ViewMappings.xaml.
        public List<ModuleItem> Modules { get; }

        /// <summary>Ban da nhom theo Group de sidebar hien label tung cum.</summary>
        public ICollectionView ModulesView { get; }

        private ModuleItem _selectedModule;
        public ModuleItem SelectedModule
        {
            get => _selectedModule;
            set
            {
                if (value != null && SetProperty(ref _selectedModule, value))
                {
                    CurrentViewModel = value.CreateViewModel();
                    OnPropertyChanged(nameof(Breadcrumb));
                }
            }
        }

        private ViewModelBase? _currentViewModel;
        /// <summary>ViewModel dang hien thi - ContentControl tu tra ra View qua ViewMappings.xaml.</summary>
        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        /// <summary>Dinh vi tren header: Trang chu / ten module dang mo.</summary>
        public string Breadcrumb => $"Trang chủ  /  {SelectedModule.Title}";

        public RelayCommand LogoutCommand { get; }

        public MainViewModel()
        {
            const string opGroup = "VẬN HÀNH";
            const string peopleGroup = "ĐỐI TƯỢNG";
            const string moneyGroup = "TÀI CHÍNH";
            const string systemGroup = "HỆ THỐNG";

            Modules =
            [
                new("", "Sơ đồ phòng", opGroup, () => new Rooms.RoomsViewModel()),
                new("", "Đặt phòng", opGroup, () => new Reservations.ReservationsViewModel()),
                new("", "Check-in / Check-out", opGroup, () => new CheckInOut.CheckInOutViewModel()),
                new("", "Khách hàng", peopleGroup, () => new PlaceholderViewModel("Khách hàng")),
                new("", "Dịch vụ", peopleGroup, () => new PlaceholderViewModel("Dịch vụ")),
                new("", "Hoá đơn", moneyGroup, () => new PlaceholderViewModel("Hoá đơn")),
                new("", "Báo cáo", moneyGroup, () => new PlaceholderViewModel("Báo cáo")),
                new("", "Người dùng", systemGroup, () => new PlaceholderViewModel("Người dùng")),
            ];

            ModulesView = new ListCollectionView(Modules);
            ModulesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ModuleItem.Group)));

            _selectedModule = Modules[0];
            _currentViewModel = _selectedModule.CreateViewModel();

            // Cho phep module khac nhay man: NavigationService.NavigateTo("Hoá đơn")
            NavigationService.Configure(title =>
            {
                var target = Modules.FirstOrDefault(m => m.Title == title);
                if (target != null)
                {
                    SelectedModule = target;
                }
            });

            LogoutCommand = new RelayCommand(_ =>
            {
                AppSession.SignOut();
                LoggedOut?.Invoke();
            });
        }
    }
}
