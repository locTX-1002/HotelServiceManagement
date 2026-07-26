using BusinessObjects.Entities;

namespace Repositories;

public interface IAuthorizationRepository
{
    Task<List<Role>> GetRolesWithPermissionsAsync();
    Task<List<Permission>> GetPermissionsAsync();
    Task<bool> ReplaceRolePermissionsAsync(int roleId, IReadOnlyCollection<int> permissionIds);
}
