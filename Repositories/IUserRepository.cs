using BusinessObjects.Entities;

namespace Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetActiveByEmailAsync(string email);
        Task<List<User>> GetAllAsync(); Task<User?> GetByIdAsync(int id); Task<Role?> GetRoleAsync(int id);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null); Task SaveAsync(User user, bool add);
        Task EnsureBootstrapAdminAsync(string fullName, string email, string passwordHash);
    }
}
