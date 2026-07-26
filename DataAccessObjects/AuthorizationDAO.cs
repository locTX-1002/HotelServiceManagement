using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public sealed class AuthorizationDAO
{
    private static readonly Lazy<AuthorizationDAO> LazyInstance = new(() => new AuthorizationDAO());
    public static AuthorizationDAO Instance => LazyInstance.Value;
    private AuthorizationDAO() { }

    public async Task<List<Role>> GetRolesWithPermissionsAsync()
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.Roles.AsNoTracking()
            .Include(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<List<Permission>> GetPermissionsAsync()
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.Permissions.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Module)
            .ThenBy(x => x.DisplayName)
            .ToListAsync();
    }

    public async Task<bool> ReplaceRolePermissionsAsync(int roleId, IReadOnlyCollection<int> permissionIds)
    {
        await using var db = HotelDbContextFactory.Create();
        if (!await db.Roles.AnyAsync(x => x.Id == roleId && x.IsActive)) return false;
        var validIds = await db.Permissions.Where(x => x.IsActive && permissionIds.Contains(x.Id))
            .Select(x => x.Id).ToListAsync();
        if (validIds.Count != permissionIds.Distinct().Count()) return false;

        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.RolePermissions.Where(x => x.RoleId == roleId).ExecuteDeleteAsync();
        db.RolePermissions.AddRange(validIds.Select(id => new RolePermission
        {
            RoleId = roleId,
            PermissionId = id,
            IsAllowed = true
        }));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
}
