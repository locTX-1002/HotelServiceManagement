using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories
{
    // Repository pattern theo khung As02: lop mong bo ra interface cho tang Service,
    // ben trong uy quyen cho DAO Singleton - de mock/de thay the khi test.
    public class UserRepository : IUserRepository
    {
        public Task<User?> GetActiveByEmailAsync(string email)
            => UserDAO.Instance.GetActiveByEmailAsync(email);
        public Task<List<User>> GetAllAsync() => UserDAO.Instance.GetAllAsync(); public Task<User?> GetByIdAsync(int id) => UserDAO.Instance.GetByIdAsync(id); public Task<Role?> GetRoleAsync(int id) => UserDAO.Instance.GetRoleAsync(id); public Task<bool> EmailExistsAsync(string email, int? id = null) => UserDAO.Instance.EmailExistsAsync(email, id); public Task SaveAsync(User x, bool add) => UserDAO.Instance.SaveAsync(x, add);
        public Task EnsureBootstrapAdminAsync(string fullName, string email, string passwordHash) => UserDAO.Instance.EnsureBootstrapAdminAsync(fullName, email, passwordHash);
    }
}
