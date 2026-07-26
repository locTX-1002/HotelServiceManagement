using BusinessObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels
{
    /// <summary>
    /// Mot muc dieu huong: icon Segoe MDL2 + ten module + nhom sidebar + ham tao ViewModel.
    /// Roles = null nghia la moi vai tro deu thay; co gia tri thi chi cac vai tro do thay muc nay.
    /// </summary>
    public record ModuleItem(
        string Icon, string Title, string Group, Func<ViewModelBase> CreateViewModel, string[]? Roles = null)
    {
        /// <summary>Vai tro hien tai co duoc thay muc nay khong.</summary>
        public bool VisibleForCurrentRole
            => Roles == null || Array.IndexOf(Roles, AppSession.RoleName) >= 0;
    }

    public class MainViewModel : ViewModelBase
    {
        /// <summary>View lang nghe de quay ve LoginWindow.</summary>
        public event Action? LoggedOut;

        public string GreetingName => AppSession.CurrentUser?.FullName ?? string.Empty;

        public string AvatarInitial
            => string.IsNullOrWhiteSpace(GreetingName) ? "?" : GreetingName.Trim()[..1].ToUpper();

        public string RoleDisplay => AppSession.RoleName switch
        {
            RoleNames.Admin => "Quản trị viên",
            RoleNames.Manager => "Quản lý",
            RoleNames.Receptionist => "Lễ tân",
            RoleNames.ServiceStaff => "Nhân viên dịch vụ",
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
        public string Breadcrumb => SelectedModule.Title == "Trang chủ"
            ? "Trang chủ"
            : $"Trang chủ  /  {SelectedModule.Title}";

        public RelayCommand LogoutCommand { get; }

        public MainViewModel()
        {
            const string homeGroup = "TỔNG QUAN";
            const string opGroup = "VẬN HÀNH";
            const string peopleGroup = "ĐỐI TƯỢNG";
            const string moneyGroup = "TÀI CHÍNH";
            const string systemGroup = "HỆ THỐNG";

            // Vai tro nao thay muc nao - dat SAT voi quyen ma service thuc su cho phep, de
            // khong ai bam vao roi moi bi tu choi. Khong ghi Roles = moi vai tro deu thay.
            string[] quanLy = [RoleNames.Admin, RoleNames.Manager];
            string[] leTan = [RoleNames.Admin, RoleNames.Manager, RoleNames.Receptionist];

            var all = new List<ModuleItem>
            {
                new("", "Trang chủ", homeGroup, () => new Home.HomeViewModel()),
                new("", "Sơ đồ phòng", opGroup, () => new Rooms.RoomsViewModel()),
                new("", "Đặt phòng", opGroup, () => new Reservations.ReservationsViewModel(), leTan),
                new("", "Nhận / Trả phòng", opGroup, () => new CheckInOut.CheckInOutViewModel(), leTan),
                new("", "Khách hàng", peopleGroup, () => new Guests.GuestsViewModel(), leTan),
                new("", "Dịch vụ", peopleGroup, () => new Services.ServicesViewModel()),
                new("", "Hoá đơn", moneyGroup, () => new Invoices.InvoicesViewModel(), leTan),
                new("", "Khuyến mãi", moneyGroup, () => new Promotions.PromotionListViewModel(), quanLy),
                new("", "Báo cáo", moneyGroup, () => new Reports.ReportViewModel(), quanLy),
                new("", "Người dùng", systemGroup, () => new Users.UserListViewModel(), [RoleNames.Admin]),
            };

            // Loc NGAY luc dung danh sach thay vi dung Filter cua CollectionView: vai tro khong
            // doi trong mot phien dang nhap nen loc mot lan la du, va SelectedModule chac chan hop le.
            Modules = all.Where(m => m.VisibleForCurrentRole).ToList();

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
