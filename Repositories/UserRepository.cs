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
    }
}
