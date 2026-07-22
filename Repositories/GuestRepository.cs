using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories
{
    public class GuestRepository : IGuestRepository
    {
        public Task<List<Guest>> SearchAsync(string? keyword) => GuestDAO.Instance.SearchAsync(keyword);
        public Task<Guest?> GetByIdAsync(int id) => GuestDAO.Instance.GetByIdAsync(id);
        public Task<bool> IdentityExistsAsync(string identityNumber, int? excludeId = null)
            => GuestDAO.Instance.IdentityExistsAsync(identityNumber, excludeId);
        public Task<Guest> AddAsync(Guest guest) => GuestDAO.Instance.AddAsync(guest);
        public Task UpdateAsync(Guest guest) => GuestDAO.Instance.UpdateAsync(guest);
    }
}
