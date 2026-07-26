using BusinessObjects.Entities;

namespace Services;

public interface IPermissionManagementService
{
    Task<ServiceResult<List<Role>>> GetRolesAsync();
    Task<ServiceResult<List<Permission>>> GetPermissionsAsync();
    Task<ServiceResult> SaveRolePermissionsAsync(int roleId, IReadOnlyCollection<int> permissionIds);
}
