using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects.Entities;

namespace Repositories
{
    public interface IGuestAccountRepository
    {
        Task<List<GuestAccount>> GetAllAsync();
        Task<GuestAccount?> GetByGuestIdAsync(int id);
        Task<GuestAccount?> GetByPhoneAsync(string p);
        Task SaveAsync(GuestAccount x, bool add);
    }
}