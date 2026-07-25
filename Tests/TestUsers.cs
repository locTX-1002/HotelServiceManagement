using BusinessObjects;
using BusinessObjects.Entities;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

/// <summary>
/// Cap tai khoan theo vai tro cho test. Database sach chi co Admin la ACTIVE
/// (3 tai khoan demo bi UserDAO.EnsureBootstrapAdminAsync khoa moi lan khoi dong),
/// nen test khong duoc gia dinh san co Receptionist/Manager/ServiceStaff.
///
/// Cach lam: neu thieu vai tro nao thi TU TAO qua UserManagementService that
/// (mat khau bam BCrypt dung chuan, khong che hash tay). Tai khoan do duoc danh dau
/// de xoa het o cuoi phien test -> chay tren may nao, DB nao cung ra ket qua giong nhau.
/// </summary>
public static class TestUsers
{
    private const string Password = "TestUser@2026";

    // Email tai khoan do chinh test tao ra - de biet duong ma don
    private static readonly List<string> Created = [];
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static readonly Dictionary<string, int> RoleIds = new()
    {
        [RoleNames.Admin] = 1, [RoleNames.Manager] = 2, [RoleNames.Receptionist] = 3, [RoleNames.ServiceStaff] = 4,
    };

    /// <summary>Dang nhap bang user THAT co vai tro yeu cau; thieu thi tao moi.</summary>
    public static async Task SignInAsync(string roleName)
        => AppSession.SignIn(await GetAsync(roleName));

    /// <summary>Lay (hoac tao) 1 user active theo vai tro, da Include(Role) de AppSession.RoleName dung.</summary>
    public static async Task<User> GetAsync(string roleName)
    {
        await Gate.WaitAsync();
        try
        {
            var existing = await FindAsync(roleName);
            if (existing != null) return existing;

            // Chua co -> tao qua service that. Phai dang nhap Admin truoc vi CreateAsync doi quyen Admin.
            var admin = await FindAsync(RoleNames.Admin)
                ?? throw new InvalidOperationException(
                    "DB khong co tai khoan Admin nao dang hoat dong. Chay app 1 lan de bootstrap admin truoc khi test.");

            var truoc = AppSession.CurrentUser;
            AppSession.SignIn(admin);
            try
            {
                var email = $"test-{roleName.ToLowerInvariant()}-{Guid.NewGuid():N}@hotel.test";
                var r = await new UserManagementService()
                    .CreateAsync($"Test {roleName}", email, Password, RoleIds[roleName]);
                if (!r.Ok) throw new InvalidOperationException($"Khong tao duoc tai khoan {roleName}: {r.Message}");
                Created.Add(email);
            }
            finally
            {
                // Tra lai phien cu de khong lam lech test dang chay
                if (truoc != null) AppSession.SignIn(truoc); else AppSession.SignOut();
            }

            return await FindAsync(roleName)
                ?? throw new InvalidOperationException($"Tao xong nhung van khong doc duoc tai khoan {roleName}.");
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task<User?> FindAsync(string roleName)
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.Users.AsNoTracking().Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Role.RoleName == roleName && u.IsActive);
    }

    /// <summary>Xoa cac tai khoan do test tao ra (khong dung toi tai khoan co san cua nguoi dung).</summary>
    public static async Task CleanupAsync()
    {
        if (Created.Count == 0) return;
        await using var db = HotelDbContextFactory.Create();
        var users = await db.Users.Where(u => Created.Contains(u.Email)).ToListAsync();
        db.Users.RemoveRange(users);
        await db.SaveChangesAsync();
        Created.Clear();
    }
}
