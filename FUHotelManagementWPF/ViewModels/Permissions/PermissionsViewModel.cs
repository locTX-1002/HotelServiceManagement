using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Permissions;

public sealed class PermissionsViewModel : ViewModelBase
{
    private readonly IPermissionManagementService _service = new PermissionManagementService();
    public ObservableCollection<Role> Roles { get; } = [];
    public ObservableCollection<PermissionOption> Permissions { get; } = [];
    public ICollectionView FilteredPermissions { get; }

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

    public PermissionsViewModel()
    {
        FilteredPermissions = new ListCollectionView(Permissions) { Filter = FilterPermission };
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync(), _ => !IsBusy);
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync(), _ => !IsBusy);
        SelectAllCommand = new RelayCommand(_ => SetAll(true));
        DeselectAllCommand = new RelayCommand(_ => SetAll(false));

        _ = LoadAsync();
    }

    private bool FilterPermission(object item)
    {
        if (item is not PermissionOption option) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var k = SearchText.Trim();
        return option.Module.Contains(k, StringComparison.OrdinalIgnoreCase)
            || option.DisplayName.Contains(k, StringComparison.OrdinalIgnoreCase)
            || option.Code.Contains(k, StringComparison.OrdinalIgnoreCase);
    }

    private void SetAll(bool selected)
    {
        foreach (PermissionOption option in FilteredPermissions)
        {
            option.IsSelected = selected;
        }
        OnPropertyChanged(nameof(GrantedCountText));
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
                        OnPropertyChanged(nameof(GrantedCountText));
                };
                Permissions.Add(opt);
            }
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
        FilteredPermissions.Refresh();
        OnPropertyChanged(nameof(GrantedCountText));
    }

    private async Task SaveAsync()
    {
        if (SelectedRole == null) return;

        // Doi quyen anh huong toi TAT CA nguoi mang vai tro do, va chi thay hau qua sau khi
        // ho dang nhap lai - nen hoi truoc, giong quy uoc hoi truoc khi xoa.
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
