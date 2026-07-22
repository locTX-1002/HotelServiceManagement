using System;
using System.Collections.Generic;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels
{
    /// <summary>Mot muc dieu huong: icon Segoe MDL2 + ten module.</summary>
    public record ModuleItem(string Icon, string Title);

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

        // Danh sach module cua he thong - moi muc se duoc thay bang UserControl
        // that khi thanh vien phu trach hoan thanh phan cua minh.
        public IReadOnlyList<ModuleItem> Modules { get; } =
        [
            new("", "Sơ đồ phòng"),
            new("", "Đặt phòng"),
            new("", "Check-in / Check-out"),
            new("", "Khách hàng"),
            new("", "Dịch vụ"),
            new("", "Hoá đơn"),
            new("", "Báo cáo"),
            new("", "Người dùng"),
        ];

        private ModuleItem _selectedModule;
        public ModuleItem SelectedModule
        {
            get => _selectedModule;
            set
            {
                if (SetProperty(ref _selectedModule, value))
                {
                    OnPropertyChanged(nameof(Breadcrumb));
                }
            }
        }

        /// <summary>Dinh vi tren header: Trang chu / ten module dang mo.</summary>
        public string Breadcrumb => $"Trang chủ  /  {SelectedModule.Title}";

        public RelayCommand LogoutCommand { get; }

        public MainViewModel()
        {
            _selectedModule = Modules[0];
            LogoutCommand = new RelayCommand(_ =>
            {
                AppSession.SignOut();
                LoggedOut?.Invoke();
            });
        }
    }
}
