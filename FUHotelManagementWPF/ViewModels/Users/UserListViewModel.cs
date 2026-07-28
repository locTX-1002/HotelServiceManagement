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
    public class RoleFilterOption : ViewModelBase
    {
        public string RoleName { get; }
        public string RawLabel { get; }

        private int _count;
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

    public class UserListViewModel : ViewModelBase
    {
        private readonly IUserManagementService _service = new UserManagementService();
        private readonly IGuestAccountService _guestAccountService = new GuestAccountService(); // Dịch vụ tài khoản khách

        private List<User> _all = [];

        public ObservableCollection<UserRow> Rows { get; } = [];

        public bool CanViewUsers => AuthorizationPolicy.CanViewUsers;
        public bool NoUserAccess => !CanViewUsers;
        public bool CanManageUsers => AuthorizationPolicy.CanManageUsers;

        public List<RoleFilterOption> RoleFilters { get; } =
        [
            new(string.Empty, "Tất cả"),
            new(RoleNames.Admin, "Quản trị viên"),
            new(RoleNames.Manager, "Quản lý"),
            new(RoleNames.Receptionist, "Lễ tân"),
            new(RoleNames.ServiceStaff, "Nhân viên dịch vụ"),
            new("Guest", "Khách hàng"), // Chip lọc "Khách hàng"
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
        public RelayCommand CreateGuestAccountCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand ResetPasswordCommand { get; }
        public RelayCommand FilterRoleCommand { get; }
        public RelayCommand ClearFilterCommand { get; }
        public AsyncRelayCommand ToggleActiveCommand { get; }
        public AsyncRelayCommand ReloadCommand { get; }

        public UserListViewModel()
        {
            AddCommand = new RelayCommand(_ => OpenEditDialog(null));
            CreateGuestAccountCommand = new RelayCommand(_ => OpenCreateGuestAccountDialog());
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
                // 1. Tải tài khoản nhân viên
                var result = await _service.GetAllAsync();
                var staffUsers = result.Ok ? (result.Data ?? []) : [];

                // 2. Tải tài khoản khách hàng
                var guestResult = await _guestAccountService.GetAllAsync();
                var guestAccounts = guestResult.Ok ? (guestResult.Data ?? []) : [];

                // 3. Chuyển GuestAccount sang dạng User giả lập để hiển thị thống nhất trong danh sách
                var guestUsers = guestAccounts.Select(g => new User
                {
                    Id = -g.GuestId, // ID âm để phân biệt với tài khoản nhân viên
                    FullName = g.Guest?.FullName ?? "Khách hàng",
                    Email = string.IsNullOrEmpty(g.Guest?.Email)
                        ? $"SĐT: {g.Guest?.PhoneNumber}"
                        : $"{g.Guest?.Email} (SĐT: {g.Guest?.PhoneNumber})",
                    IsActive = true,
                    Role = new Role { RoleName = "Guest", Description = "Khách hàng" }
                });

                _all = staffUsers.Concat(guestUsers).ToList();
                ApplyFilter();
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

        private void ApplyFilter()
        {
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
            if (parameter is not RoleFilterOption option) return;

            _roleFilter = option.RoleName;
            foreach (var item in RoleFilters)
            {
                item.IsSelected = ReferenceEquals(item, option);
            }
            ApplyFilter();
        }

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
                Notify.Error($"Không mở được form tài khoản: {ex.Message}");
            }
        }

        private async void OpenCreateGuestAccountDialog()
        {
            try
            {
                var dialog = new CreateGuestAccountDialog();
                SetOwner(dialog);
                if (dialog.ShowDialog() == true)
                {
                    await LoadAsync();
                }
            }
            catch (Exception ex)
            {
                Notify.Error($"Không mở được form tạo tài khoản khách: {ex.Message}");
            }
        }

        private void OpenResetPasswordDialog(User? target)
        {
            if (target == null) return;

            var dialog = new ResetPasswordDialog(new ResetPasswordDialogViewModel(target));
            SetOwner(dialog);
            dialog.ShowDialog();
        }

        private async Task ToggleActiveAsync(object? parameter)
        {
            var row = RowOf(parameter);
            if (row == null || !row.CanToggleActive) return;

            var willLock = row.IsActive;
            var question = willLock
                ? $"Khoá tài khoản \"{row.FullName}\"?\n\nTài khoản này sẽ không đăng nhập được cho tới khi được mở khoá."
                : $"Mở khoá tài khoản \"{row.FullName}\"?\n\nTài khoản này sẽ đăng nhập lại được ngay.";

            var caption = willLock ? "Khoá tài khoản" : "Mở khoá tài khoản";
            var owner = RoomMapViewModel.ActiveWindow();
            var answer = owner == null
                ? MessageBox.Show(question, caption, MessageBoxButton.YesNo, MessageBoxImage.Question)
                : MessageBox.Show(owner, question, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes) return;

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