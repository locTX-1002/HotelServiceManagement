using System.Collections.ObjectModel;
using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;
using Services;

namespace FUHotelManagementWPF.ViewModels.Permissions;

public sealed class PermissionsViewModel : ViewModelBase
{
    private readonly IPermissionManagementService _service = new PermissionManagementService();
    public ObservableCollection<Role> Roles { get; } = [];
    public ObservableCollection<PermissionOption> Permissions { get; } = [];

    private Role? _selectedRole;
    public Role? SelectedRole
    {
        get => _selectedRole;
        set
        {
            if (SetProperty(ref _selectedRole, value)) ApplySelectedRole();
        }
    }

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }

    public PermissionsViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
        _ = LoadAsync();
    }

    private async Task LoadAsync()
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
        foreach (var permission in permissions.Data) Permissions.Add(new PermissionOption(permission));
        SelectedRole = Roles.FirstOrDefault(x => x.Id == selectedId) ?? Roles.FirstOrDefault();
        ApplySelectedRole();
    }

    private void ApplySelectedRole()
    {
        var selectedIds = SelectedRole?.RolePermissions
            .Where(x => x.IsAllowed)
            .Select(x => x.PermissionId)
            .ToHashSet() ?? [];
        foreach (var permission in Permissions)
            permission.IsSelected = selectedIds.Contains(permission.Id);
    }

    private async Task SaveAsync()
    {
        if (SelectedRole == null) return;
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
}
