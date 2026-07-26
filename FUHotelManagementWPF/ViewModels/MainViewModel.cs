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
    /// Permissions = null nghia la moi nhan vien deu thay; co gia tri thi can it nhat mot quyen.
    /// </summary>
    public record ModuleItem(
        string Icon, string Title, string Group, Func<ViewModelBase> CreateViewModel, string[]? Permissions = null)
    {
        /// <summary>Vai tro hien tai co duoc thay muc nay khong.</summary>
        public bool VisibleForCurrentRole
            => Permissions == null || Permissions.Any(AuthorizationPolicy.HasPermission);
    }

    public class MainViewModel : ViewModelBase
    {
        /// <summary>View lang nghe de quay ve LoginWindow.</summary>
        public event Action? LoggedOut;

        public string GreetingName => AppSession.CurrentUser?.FullName ?? string.Empty;

        public string AvatarInitial
            => string.IsNullOrWhiteSpace(GreetingName) ? "?" : GreetingName.Trim()[..1].ToUpper();

        public string RoleDisplay => AppSession.CurrentUser?.Role?.DisplayName ?? AppSession.RoleName;

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
            _ = SweepStaleReservationsAsync();

            const string homeGroup = "TỔNG QUAN";
            const string opGroup = "VẬN HÀNH";
            const string peopleGroup = "ĐỐI TƯỢNG";
            const string moneyGroup = "TÀI CHÍNH";
            const string systemGroup = "HỆ THỐNG";

            var all = new List<ModuleItem>
            {
                new("", "Trang chủ", homeGroup, () => new Home.HomeViewModel()),
                new("", "Sơ đồ phòng", opGroup, () => new Rooms.RoomsViewModel(), [PermissionCodes.RoomView]),
                new("", "Đặt phòng", opGroup, () => new Reservations.ReservationsViewModel(), [PermissionCodes.ReservationView]),
                new("", "Nhận / Trả phòng", opGroup, () => new CheckInOut.CheckInOutViewModel(), [PermissionCodes.StayCheckIn, PermissionCodes.StayCheckOut]),
                new("", "Khách hàng", peopleGroup, () => new Guests.GuestsViewModel(), [PermissionCodes.GuestView]),
                new("", "Dịch vụ", peopleGroup, () => new Services.ServicesViewModel(), [PermissionCodes.ServiceCatalogManage, PermissionCodes.ServiceOrderCreate, PermissionCodes.ServiceOrderProcess]),
                new("", "Hoá đơn", moneyGroup, () => new Invoices.InvoicesViewModel(), [PermissionCodes.InvoiceView]),
                new("", "Khuyến mãi", moneyGroup, () => new Promotions.PromotionListViewModel(), [PermissionCodes.PromotionManage]),
                new("", "Báo cáo", moneyGroup, () => new Reports.ReportViewModel(), [PermissionCodes.ReportView]),
                new("", "Phê duyệt", moneyGroup, () => new Approvals.ApprovalsViewModel(),
                    [PermissionCodes.ReservationCancelApprove, PermissionCodes.InvoiceDiscountApprove,
                     PermissionCodes.InvoiceCancelApprove, PermissionCodes.PaymentVoidApprove]),
                new("", "Người dùng", systemGroup, () => new Users.UserListViewModel(), [PermissionCodes.UserView]),
                new("", "Phân quyền", systemGroup, () => new Permissions.PermissionsViewModel(),
                    [PermissionCodes.PermissionManage]),
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
        /// <summary>
        /// Don don treo ngay khi vao app: don da qua ngay nhan phong ma khach khong den thi
        /// chuyen sang Khong den. Khong lam thi no giu cho mai - truy van phong trong van
        /// tinh la ban nen phong khong ban lai duoc, con lich phong ve mot thanh cua ky nghi
        /// da troi qua.
        ///
        /// Loi o buoc nay khong duoc lam hong man hinh: don dep hong thi lan sau quet lai.
        /// </summary>
        private static async Task SweepStaleReservationsAsync()
        {
            try
            {
                var swept = await new ReservationService().SweepNoShowAsync();
                if (swept > 0)
                {
                    Notify.Info($"Đã tự chuyển {swept} đơn quá hạn sang Không đến.");
                }
            }
            catch (Exception)
            {
                // Co y nuot: khong the vi don dep that bai ma chan nguoi dung vao app.
            }
        }

    }
}
