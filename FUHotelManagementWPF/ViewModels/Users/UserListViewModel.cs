using BusinessObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using FUHotelManagementWPF.ViewModels.Rooms;
using FUHotelManagementWPF.Views.Dialogs;
using Services;

namespace FUHotelManagementWPF.ViewModels.Users
{
    /// <summary>Mot chip loc theo vai tro. IsSelected de chip dang chon in dam.</summary>
    public class RoleFilterOption : ViewModelBase
    {
        /// <summary>Rong = "Tat ca" (khong loc).</summary>
        public string RoleName { get; }

        /// <summary>Ten vai tro chua kem so luong - de ghep lai moi lan Count doi.</summary>
        public string RawLabel { get; }

        private int _count;

        /// <summary>
        /// So tai khoan dang mang vai tro nay. Hien ngay tren chip de Admin biet
        /// nhan vao co gi khong, thay vi bam thu roi thay bang trong.
        /// </summary>
        public int Count
        {
            get => _count;
            set { if (SetProperty(ref _count, value)) { OnPropertyChanged(nameof(Label)); } }
        }

        public string Label => Count > 0 ? $"{RawLabel} ({Count})" : RawLabel;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public RoleFilterOption(string roleName, string label)
        {
            RoleName = roleName;
            RawLabel = label;
        }
    }

    /// <summary>
    /// Module Nguoi dung: danh sach tai khoan nhan vien (chi Admin) + tim kiem,
    /// loc vai tro, them/sua, khoa/mo, dat lai mat khau.
    /// </summary>
    public class UserListViewModel : ViewModelBase
    {
        private readonly IUserManagementService _service = new UserManagementService();

        /// <summary>
        /// Ban goc tai ve tu service. Loc/tim lam ngay tren bo nho vi service chi co
        /// GetAllAsync (khong co ham tim theo tu khoa) va so nhan vien khach san rat it.
        /// </summary>
        private List<User> _all = [];

        public ObservableCollection<UserRow> Rows { get; } = [];

        /// <summary>
        /// Mo duoc man: dung DUNG dieu kien ma menu dung (<c>user.view</c>), khong thi
        /// muc menu hien ra ma bam vao lai bao khong co quyen.
        /// </summary>
        public bool CanViewUsers => AuthorizationPolicy.CanViewUsers;
        public bool NoUserAccess => !CanViewUsers;

        /// <summary>Them / sua / khoa / dat lai mat khau - chat hon xem, doi <c>user.manage</c>.</summary>
        public bool CanManageUsers => AuthorizationPolicy.CanManageUsers;

        public List<RoleFilterOption> RoleFilters { get; } =
        [
            new(string.Empty, "Tất cả"),
            new(RoleNames.Admin, "Quản trị viên"),
            new(RoleNames.Manager, "Quản lý"),
            new(RoleNames.Receptionist, "Lễ tân"),
            new(RoleNames.ServiceStaff, "Nhân viên dịch vụ"),
        ];

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) { ApplyFilter(); } }
        }

        private string _roleFilter = string.Empty;

        private UserRow? _selectedRow;
        public UserRow? SelectedRow
        {
            get => _selectedRow;
            set { if (SetProperty(ref _selectedRow, value)) { OnPropertyChanged(nameof(HasSelection)); } }
        }
        public bool HasSelection => _selectedRow != null;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (SetProperty(ref _isLoading, value)) { OnPropertyChanged(nameof(IsEmpty)); } }
        }

        // Trang thai loi: giu rieng voi rong de nguoi dung biet la mat ket noi
        // chu khong phai "chua co tai khoan nao".
        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }
        public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);

        public bool IsEmpty => CanViewUsers && !IsLoading && !HasError && Rows.Count == 0;

        public string TotalText
        {
            get
            {
                var locked = Rows.Count(r => !r.IsActive);
                return locked == 0
                    ? $"{Rows.Count} tài khoản"
                    : $"{Rows.Count} tài khoản · {locked} đã khoá";
            }
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand ResetPasswordCommand { get; }
        public RelayCommand FilterRoleCommand { get; }
        public RelayCommand ClearFilterCommand { get; }
        public AsyncRelayCommand ToggleActiveCommand { get; }
        public AsyncRelayCommand ReloadCommand { get; }

        public UserListViewModel()
        {
            AddCommand = new RelayCommand(_ => OpenEditDialog(null));
            // Nhan tham so UserRow de nut ngay tren dong lam viec duoc luon: bam nut cua
            // dong nao thi sua dong do, khong phai chon dong roi moi bam nut o thanh tren.
            EditCommand = new RelayCommand(p => OpenEditDialog(RowOf(p)?.User));
            ResetPasswordCommand = new RelayCommand(p => OpenResetPasswordDialog(RowOf(p)?.User));
            FilterRoleCommand = new RelayCommand(SelectRoleFilter);
            ClearFilterCommand = new RelayCommand(_ => ClearFilters());
            ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync);
            ReloadCommand = new AsyncRelayCommand(_ => LoadAsync());

            RoleFilters[0].IsSelected = true;

            if (CanViewUsers)
            {
                _ = LoadAsync();
            }
        }

        /// <summary>Dong duoc truyen vao tu nut tren luoi; khong co thi lay dong dang chon.</summary>
        private UserRow? RowOf(object? parameter) => parameter as UserRow ?? SelectedRow;

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectRoleFilter(RoleFilters[0]);
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var result = await _service.GetAllAsync();
                if (result.Ok)
                {
                    _all = result.Data ?? [];
                    ApplyFilter();
                }
                else
                {
                    _all = [];
                    ApplyFilter();
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception)
            {
                _all = [];
                ApplyFilter();
                ErrorMessage = "Không tải được danh sách tài khoản. Kiểm tra kết nối SQL Server rồi thử lại.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Loc tren danh sach da tai: theo tu khoa (ten/email) + theo vai tro dang chon.
        private void ApplyFilter()
        {
            // Dem tren _all chu khong tren Rows: so tren chip phai la tong so tai khoan
            // cua vai tro do, khong doi theo tu khoa dang go.
            foreach (var filter in RoleFilters)
            {
                filter.Count = filter.RoleName.Length == 0
                    ? _all.Count
                    : _all.Count(u => u.Role?.RoleName == filter.RoleName);
            }

            var keyword = _searchText.Trim();
            var keepId = SelectedRow?.User.Id;

            var query = _all.AsEnumerable();
            if (_roleFilter.Length > 0)
            {
                query = query.Where(u => u.Role?.RoleName == _roleFilter);
            }
            if (keyword.Length > 0)
            {
                query = query.Where(u =>
                    u.FullName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
                    || u.Email.Contains(keyword, StringComparison.CurrentCultureIgnoreCase));
            }

            Rows.Clear();
            foreach (var u in query)
            {
                Rows.Add(new UserRow(u));
            }

            SelectedRow = Rows.FirstOrDefault(r => r.User.Id == keepId);
            OnPropertyChanged(nameof(TotalText));
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void SelectRoleFilter(object? parameter)
        {
            if (parameter is not RoleFilterOption option)
            {
                return;
            }

            _roleFilter = option.RoleName;
            foreach (var item in RoleFilters)
            {
                item.IsSelected = ReferenceEquals(item, option);
            }
            ApplyFilter();
        }

        /// <summary>
        /// Gan Owner qua ham rieng vi ActiveWindow() co the tra ve chinh dialog dang mo
        /// (WPF nem "Window cannot be its own owner") hoac null luc chua co cua so nao.
        /// </summary>
        private static void SetOwner(Window dialog)
        {
            var owner = RoomMapViewModel.ActiveWindow();
            if (owner != null && !ReferenceEquals(owner, dialog))
            {
                dialog.Owner = owner;
            }
        }

        private async void OpenEditDialog(User? existing)
        {
            try
            {
                var dialog = new UserEditDialog(new UserEditDialogViewModel(existing));
                SetOwner(dialog);
                if (dialog.ShowDialog() == true)
                {
                    await LoadAsync();
                }
            }
            catch (Exception ex)
            {
                // async void: loi khong ai bat duoc nua nen phai chan tai day, khong thi tat app.
                Notify.Error($"Không mở được form tài khoản: {ex.Message}");
            }
        }

        private void OpenResetPasswordDialog(User? target)
        {
            if (target == null)
            {
                return;
            }

            var dialog = new ResetPasswordDialog(new ResetPasswordDialogViewModel(target));
            SetOwner(dialog);
            dialog.ShowDialog();
        }

        // Khoa / mo khoa tai khoan dang chon. Dung MessageBox vi day la hanh dong
        // chan nguoi khac dang nhap - can hoi lai truoc khi lam.
        private async Task ToggleActiveAsync(object? parameter)
        {
            var row = RowOf(parameter);
            if (row == null || !row.CanToggleActive)
            {
                return;
            }

            var willLock = row.IsActive;
            var question = willLock
                ? $"Khoá tài khoản \"{row.FullName}\"?\n\nNhân viên này sẽ không đăng nhập được cho tới khi được mở khoá."
                : $"Mở khoá tài khoản \"{row.FullName}\"?\n\nNhân viên này sẽ đăng nhập lại được ngay.";

            var caption = willLock ? "Khoá tài khoản" : "Mở khoá tài khoản";
            var owner = RoomMapViewModel.ActiveWindow();
            // Tach hai nhanh vi MessageBox.Show khong nhan owner null.
            var answer = owner == null
                ? MessageBox.Show(question, caption, MessageBoxButton.YesNo, MessageBoxImage.Question)
                : MessageBox.Show(owner, question, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await _service.SetActiveAsync(row.User.Id, !row.IsActive);
            if (result.Ok)
            {
                Notify.Success(result.Message);
                await LoadAsync();
            }
            else
            {
                Notify.Error(result.Message);
            }
        }
    }
}
