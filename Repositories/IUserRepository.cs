using BusinessObjects.Entities;

namespace Repositories
{
    public interface IUserRepository
    {
        User? GetActiveByEmail(string email);
    }
}
