using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using BusinessObjects;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Permissions;

public class ModuleFilterOption : ViewModelBase
{
    public string ModuleName { get; }
    public string RawLabel { get; }

    private int _grantedCount;
    public int GrantedCount
    {
        get => _grantedCount;
        set { if (SetProperty(ref _grantedCount, value)) OnPropertyChanged(nameof(Label)); }
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set { if (SetProperty(ref _totalCount, value)) OnPropertyChanged(nameof(Label)); }
    }

    public string Label => TotalCount > 0
        ? (ModuleName.Length == 0 ? $"{RawLabel} ({TotalCount})" : $"{RawLabel} ({GrantedCount}/{TotalCount})")
        : RawLabel;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ModuleFilterOption(string moduleName, string label)
    {
        ModuleName = moduleName;
        RawLabel = label;
    }
}

public sealed class PermissionsViewModel : ViewModelBase
{
    private readonly IPermissionManagementService _service = new PermissionManagementService();
    public ObservableCollection<Role> Roles { get; } = [];
    public ObservableCollection<PermissionOption> Permissions { get; } = [];
    public ObservableCollection<ModuleFilterOption> ModuleFilters { get; } = [];
    public ICollectionView FilteredPermissions { get; }

    private string _selectedModule = string.Empty;

    private Role? _selectedRole;
    public Role? SelectedRole
    {
        get => _selectedRole;
        set
        {
            if (SetProperty(ref _selectedRole, value))
            {
                ApplySelectedRole();
                OnPropertyChanged(nameof(RoleSubtitle));
            }
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilteredPermissions.Refresh();
                OnPropertyChanged(nameof(GrantedCountText));
            }
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string GrantedCountText
    {
        get
        {
            var granted = Permissions.Count(x => x.IsSelected);
            return $"Đang cấp {granted}/{Permissions.Count} quyền hạn";
        }
    }

    public string RoleSubtitle => SelectedRole != null
        ? $"Thiết lập danh sách quyền hạn cho vai trò: {SelectedRole.DisplayName}"
        : "Chọn một vai trò để thiết lập quyền hạn";

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand DeselectAllCommand { get; }
    public RelayCommand FilterModuleCommand { get; }

    public PermissionsViewModel()
    {
        FilteredPermissions = new ListCollectionView(Permissions) { Filter = FilterPermission };
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync(), _ => !IsBusy);
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync(), _ => !IsBusy);
        SelectAllCommand = new RelayCommand(_ => SetAll(true));
        DeselectAllCommand = new RelayCommand(_ => SetAll(false));
        FilterModuleCommand = new RelayCommand(SelectModuleFilter);

        _ = LoadAsync();
    }

    private bool FilterPermission(object item)
    {
        if (item is not PermissionOption option) return false;

        if (_selectedModule.Length > 0 && !string.Equals(option.Module, _selectedModule, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var k = SearchText.Trim();
        return option.Module.Contains(k, StringComparison.OrdinalIgnoreCase)
            || option.DisplayName.Contains(k, StringComparison.OrdinalIgnoreCase)
            || option.Code.Contains(k, StringComparison.OrdinalIgnoreCase);
    }

    private void SelectModuleFilter(object? parameter)
    {
        if (parameter is not ModuleFilterOption option) return;
        _selectedModule = option.ModuleName;
        foreach (var item in ModuleFilters)
        {
            item.IsSelected = ReferenceEquals(item, option);
        }
        FilteredPermissions.Refresh();
    }

    private void RefreshModuleCounters()
    {
        foreach (var filter in ModuleFilters)
        {
            if (filter.ModuleName.Length == 0)
            {
                filter.TotalCount = Permissions.Count;
                filter.GrantedCount = Permissions.Count(p => p.IsSelected);
            }
            else
            {
                var perms = Permissions.Where(p => string.Equals(p.Module, filter.ModuleName, StringComparison.OrdinalIgnoreCase)).ToList();
                filter.TotalCount = perms.Count;
                filter.GrantedCount = perms.Count(p => p.IsSelected);
            }
        }
        OnPropertyChanged(nameof(GrantedCountText));
    }

    public static Action? LogoutRequested { get; set; }

    private void SetAll(bool selected)
    {
        if (!selected)
        {
            foreach (PermissionOption option in FilteredPermissions)
            {
                option.IsSelected = false;
            }
            RefreshModuleCounters();
            return;
        }

        // Cấp tất cả thông minh
        if (_selectedModule.Length > 0)
        {
            // Đang lọc theo Module cụ thể: cấp tất cả các quyền thuộc Module đang lọc
            foreach (PermissionOption option in FilteredPermissions)
            {
                option.IsSelected = true;
            }
        }
        else
        {
            // Đang xem "Tất cả": Cấp quyền an toàn theo quy định Tách nhiệm vụ (Separation of Duties)
            var isSystemAdminRole = string.Equals(SelectedRole?.RoleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase)
                || (SelectedRole?.RolePermissions.Any(x => x.IsAllowed && string.Equals(x.Permission?.PermissionCode, PermissionCodes.PermissionManage, StringComparison.OrdinalIgnoreCase)) ?? false);

            if (isSystemAdminRole)
            {
                // Vai trò quản trị hệ thống: chỉ cấp quyền hệ thống (user, permission, audit)
                foreach (var p in Permissions)
                {
                    var code = p.Code.ToLowerInvariant();
                    p.IsSelected = code.StartsWith("user.") || code.StartsWith("permission.") || code.StartsWith("audit.");
                }
            }
            else
            {
                // Vai trò nghiệp vụ: loại trừ các quyền phê duyệt trùng với quyền yêu cầu
                var excludedConflictCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    PermissionCodes.ReservationCancelApprove,
                    PermissionCodes.InvoiceDiscountApprove,
                    PermissionCodes.InvoiceCancelApprove,
                    PermissionCodes.PaymentVoidApprove,
                    PermissionCodes.RoomMaintenanceApprove
                };

                foreach (var p in Permissions)
                {
                    var code = p.Code.ToLowerInvariant();
                    if (code == PermissionCodes.PermissionManage || code == PermissionCodes.UserManage)
                    {
                        p.IsSelected = false;
                    }
                    else if (excludedConflictCodes.Contains(p.Code))
                    {
                        p.IsSelected = false;
                    }
                    else
                    {
                        p.IsSelected = true;
                    }
                }
            }
        }

        RefreshModuleCounters();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var roles = await _service.GetRolesAsync();
            var permissions = await _service.GetPermissionsAsync();
            if (!roles.Ok || roles.Data == null)
            {
                Notify.Error(roles.Message);
                return;
            }
            if (!permissions.Ok || permissions.Data == null)
            {
                Notify.Error(permissions.Message);
                return;
            }

            var selectedId = SelectedRole?.Id;
            Roles.Clear();
            foreach (var role in roles.Data) Roles.Add(role);

            Permissions.Clear();
            foreach (var permission in permissions.Data)
            {
                var opt = new PermissionOption(permission);
                opt.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(PermissionOption.IsSelected))
                        RefreshModuleCounters();
                };
                Permissions.Add(opt);
            }

            // Build Module Chips
            var modules = Permissions.Select(p => p.Module).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(m => m);
            ModuleFilters.Clear();
            ModuleFilters.Add(new ModuleFilterOption(string.Empty, "Tất cả") { IsSelected = true });
            foreach (var mod in modules)
            {
                ModuleFilters.Add(new ModuleFilterOption(mod, mod));
            }
            _selectedModule = string.Empty;

            SelectedRole = Roles.FirstOrDefault(x => x.Id == selectedId) ?? Roles.FirstOrDefault();
            ApplySelectedRole();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySelectedRole()
    {
        var selectedIds = SelectedRole?.RolePermissions
            .Where(x => x.IsAllowed)
            .Select(x => x.PermissionId)
            .ToHashSet() ?? [];
        foreach (var permission in Permissions)
            permission.IsSelected = selectedIds.Contains(permission.Id);

        RefreshModuleCounters();
        FilteredPermissions.Refresh();
    }

    private async Task SaveAsync()
    {
        if (SelectedRole == null) return;

        var granted = Permissions.Count(x => x.IsSelected);
        var question = $"Lưu {granted}/{Permissions.Count} quyền cho vai trò \"{SelectedRole.DisplayName}\"?\n\n"
                     + "Mọi tài khoản thuộc vai trò này sẽ áp dụng quyền mới sau khi đăng nhập lại.";
        if (MessageBox.Show(question, "Lưu phân quyền", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var ids = Permissions.Where(x => x.IsSelected).Select(x => x.Id).ToArray();
            var result = await _service.SaveRolePermissionsAsync(SelectedRole.Id, ids);
            if (result.Ok)
            {
                Notify.Success(result.Message);
                await LoadAsync();

                var logoutQuestion = "Đã lưu phân quyền thành công.\n\n"
                                   + "Bạn có muốn đăng xuất ngay bây giờ để hệ thống áp dụng quyền mới không?";
                if (MessageBox.Show(logoutQuestion, "Áp dụng quyền mới", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.Yes)
                {
                    LogoutRequested?.Invoke();
                }
            }
            else
            {
                Notify.Error(result.Message);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
