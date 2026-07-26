using BusinessObjects.Entities;
using FUHotelManagementWPF.MvvmCore;

namespace FUHotelManagementWPF.ViewModels.Permissions;

public sealed class PermissionOption : ViewModelBase
{
    public Permission Permission { get; }
    public int Id => Permission.Id;
    public string Module => Permission.Module;
    public string DisplayName => Permission.DisplayName;
    public string Code => Permission.PermissionCode;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public PermissionOption(Permission permission) => Permission = permission;
}
