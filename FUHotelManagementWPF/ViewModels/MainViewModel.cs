using System;
using System.Collections.Generic;
using System.Linq;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels
{
    /// <summary>Mot muc dieu huong: icon Segoe MDL2 + ten module + ham tao ViewModel cua module do.</summary>
    public record ModuleItem(string Icon, string Title, Func<ViewModelBase> CreateViewModel);

    public class MainViewModel : ViewModelBase
    {
        /// <summary>View lang nghe de quay ve LoginWindow.</summary>
        public event Action? LoggedOut;

        public string GreetingName => AppSession.CurrentUser?.FullName ?? string.Empty;

        public string RoleDisplay => AppSession.RoleName switch
        {
            "Admin" => "Quản trị viên",
            "Manager" => "Quản lý",
            "Receptionist" => "Lễ tân",
            "ServiceStaff" => "Nhân viên dịch vụ",
            _ => AppSession.RoleName,
        };

        // Danh sach module: thanh vien lam xong module nao thi doi factory cua module do
        // sang ViewModel that (vd: new("", "Sơ đồ phòng", () => new RoomMapViewModel()))
        // va them 1 dong DataTemplate vao Views/ViewMappings.xaml. Chi vay la xong.
        public IReadOnlyList<ModuleItem> Modules { get; }

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
            Modules =
            [
                new("", "Sơ đồ phòng", () => new PlaceholderViewModel("Sơ đồ phòng")),
                new("", "Đặt phòng", () => new PlaceholderViewModel("Đặt phòng")),
                new("", "Check-in / Check-out", () => new PlaceholderViewModel("Check-in / Check-out")),
                new("", "Khách hàng", () => new PlaceholderViewModel("Khách hàng")),
                new("", "Dịch vụ", () => new PlaceholderViewModel("Dịch vụ")),
                new("", "Hoá đơn", () => new PlaceholderViewModel("Hoá đơn")),
                new("", "Báo cáo", () => new PlaceholderViewModel("Báo cáo")),
                new("", "Người dùng", () => new PlaceholderViewModel("Người dùng")),
            ];

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
