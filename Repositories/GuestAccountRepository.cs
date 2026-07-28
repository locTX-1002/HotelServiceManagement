using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories;

public sealed class GuestAccountRepository : IGuestAccountRepository
{
    public Task<List<GuestAccount>> GetAllAsync() => GuestAccountDAO.Instance.GetAllAsync();
    public Task<GuestAccount?> GetByGuestIdAsync(int id) => GuestAccountDAO.Instance.GetByGuestIdAsync(id);
    public Task<GuestAccount?> GetByPhoneAsync(string p) => GuestAccountDAO.Instance.GetByPhoneAsync(p);
    public Task SaveAsync(GuestAccount x, bool add) => GuestAccountDAO.Instance.SaveAsync(x, add);
}