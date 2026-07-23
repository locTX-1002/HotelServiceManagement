using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects
{
    /// <summary>
    /// DAO tang truy cap du lieu cho User - Singleton pattern theo dung khung diem cua de
    /// (moi DAO 1 Instance duy nhat, Repository chi bo ra goi lai). Moi thao tac mo 1 DbContext
    /// ngan han rieng de tranh giu context song lau trong app desktop (de dinh cache cu/loi track).
    /// QUY UOC NHOM: moi ham DAO deu la async (...Async) - cam ExecuteSync/.Result lam dung hinh UI.
    /// </summary>
    public class UserDAO
    {
        private static UserDAO? _instance;
        private static readonly object _lock = new();

        private UserDAO() { }

        public static UserDAO Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new UserDAO();
                }
            }
        }

        /// <summary>Tim user con hoat dong theo email (khong phan biet hoa thuong), kem Role de phan quyen.</summary>
        public async Task<User?> GetActiveByEmailAsync(string email)
        {
            var normalized = email.Trim().ToLower();
            await using var context = HotelDbContextFactory.Create();
            return await context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IsActive && u.Email.ToLower() == normalized);
        }

        public async Task<List<User>> GetAllAsync() { await using var c = HotelDbContextFactory.Create(); return await c.Users.AsNoTracking().Include(x => x.Role).OrderBy(x => x.Id).ToListAsync(); }
        public async Task<User?> GetByIdAsync(int id) { await using var c = HotelDbContextFactory.Create(); return await c.Users.AsNoTracking().Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == id); }
        public async Task<Role?> GetRoleAsync(int id) { await using var c = HotelDbContextFactory.Create(); return await c.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id); }
        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null) { var n = email.Trim().ToLower(); await using var c = HotelDbContextFactory.Create(); return await c.Users.AnyAsync(x => x.Email.ToLower() == n && (excludeId == null || x.Id != excludeId)); }
        public async Task SaveAsync(User x, bool add) { await using var c = HotelDbContextFactory.Create(); x.Role = null!; if (add) c.Users.Add(x); else c.Users.Update(x); await c.SaveChangesAsync(); }
        public async Task EnsureBootstrapAdminAsync(string fullName, string email, string passwordHash)
        {
            await using var c = HotelDbContextFactory.Create();
            var adminRole = await c.Roles.FirstAsync(r => r.RoleName == "Admin");
            var admin = await c.Users.FirstOrDefaultAsync(u => u.Email == email)
                ?? await c.Users.FirstOrDefaultAsync(u => u.RoleId == adminRole.Id);
            if (admin == null)
            {
                c.Users.Add(new User { FullName = fullName, Email = email, PasswordHash = passwordHash, RoleId = adminRole.Id, IsActive = true });
            }
            else
            {
                admin.FullName = fullName;
                admin.Email = email;
                admin.PasswordHash = passwordHash;
                admin.RoleId = adminRole.Id;
                admin.IsActive = true;
            }
            var demoEmails = new[] { "manager@hotel.com", "receptionist@hotel.com", "service@hotel.com" };
            var demoUsers = await c.Users.Where(u => demoEmails.Contains(u.Email)).ToListAsync();
            foreach (var demoUser in demoUsers) demoUser.IsActive = false;
            await c.SaveChangesAsync();
        }
    }
}
