using BusinessObjects.Entities;

namespace Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetActiveByEmailAsync(string email);
        Task<List<User>> GetAllAsync(); Task<User?> GetByIdAsync(int id); Task<Role?> GetRoleAsync(int id);

        /// <summary>Toan bo vai tro trong DB - man Nhan su do ra ComboBox thay vi ghi cung.</summary>
        Task<List<Role>> GetRolesAsync();
        Task<bool> EmailExistsAsync(string email, int? excludeId = null); Task SaveAsync(User user, bool add);
        Task EnsureBootstrapAdminAsync(string fullName, string email, string passwordHash);
    }
}
