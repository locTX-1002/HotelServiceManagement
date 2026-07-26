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
        public string RoleName { get; }
        public string RawLabel { get; }

        private int _count;
        public int Count
        {
            get => _count;
            set
            {
                if (SetProperty(ref _count, value))
                {
                    OnPropertyChanged(nameof(Label));
                }
            }
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

        private List<User> _all = [];

        public ObservableCollection<UserRow> Rows { get; } = [];

        public bool IsAdmin => AppSession.RoleName == RoleNames.Admin;
        public bool IsNotAdmin => !IsAdmin;

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

        public bool IsEmpty => IsAdmin && !IsLoading && !HasError && Rows.Count == 0;

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
            EditCommand = new RelayCommand(p => OpenEditDialog((p as UserRow)?.User ?? SelectedRow?.User));
            ResetPasswordCommand = new RelayCommand(p => OpenResetPasswordDialog((p as UserRow)?.User ?? SelectedRow?.User));
            FilterRoleCommand = new RelayCommand(SelectRoleFilter);
            ClearFilterCommand = new RelayCommand(_ => ClearFilters());
            ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync);
            ReloadCommand = new AsyncRelayCommand(_ => LoadAsync());

            RoleFilters[0].IsSelected = true;

            if (IsAdmin)
            {
                _ = LoadAsync();
            }
        }

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

        private void ApplyFilter()
        {
            foreach (var filter in RoleFilters)
            {
                filter.Count = string.IsNullOrEmpty(filter.RoleName)
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

        private async void OpenEditDialog(User? existing)
        {
            var dialog = new UserEditDialog(new UserEditDialogViewModel(existing))
            {
                Owner = RoomMapViewModel.ActiveWindow(),
            };
            if (dialog.ShowDialog() == true)
            {
                await LoadAsync();
            }
        }

        private void OpenResetPasswordDialog(User? target)
        {
            var user = target ?? SelectedRow?.User;
            if (user == null)
            {
                return;
            }

            new ResetPasswordDialog(new ResetPasswordDialogViewModel(user))
            {
                Owner = RoomMapViewModel.ActiveWindow(),
            }.ShowDialog();
        }

        private async Task ToggleActiveAsync(object? param)
        {
            var row = (param as UserRow) ?? SelectedRow;
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
