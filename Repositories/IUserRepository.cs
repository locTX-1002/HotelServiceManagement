using BusinessObjects.Entities;

namespace Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetActiveByEmailAsync(string email);
    }
}
