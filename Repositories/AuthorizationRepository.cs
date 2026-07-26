using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories;

public sealed class AuthorizationRepository : IAuthorizationRepository
{
    public Task<List<Role>> GetRolesWithPermissionsAsync()
        => AuthorizationDAO.Instance.GetRolesWithPermissionsAsync();
    public Task<List<Permission>> GetPermissionsAsync()
        => AuthorizationDAO.Instance.GetPermissionsAsync();
    public Task<bool> ReplaceRolePermissionsAsync(int roleId, IReadOnlyCollection<int> permissionIds)
        => AuthorizationDAO.Instance.ReplaceRolePermissionsAsync(roleId, permissionIds);
}
