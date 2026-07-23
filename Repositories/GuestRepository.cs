using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories;

public sealed class GuestRepository : IGuestRepository
{
    public Task<List<Guest>> GetAllAsync() => GuestDAO.Instance.GetAllAsync();
    public Task<Guest?> GetByIdAsync(int id) => GuestDAO.Instance.GetByIdAsync(id);
    public Task<List<Guest>> SearchAsync(string keyword) => GuestDAO.Instance.SearchAsync(keyword);
    public Task<bool> IdentityNumberExistsAsync(string value, int? excludeId = null)
        => GuestDAO.Instance.IdentityNumberExistsAsync(value, excludeId);
    public Task<bool> HasReservationsAsync(int guestId) => GuestDAO.Instance.HasReservationsAsync(guestId);
    public Task AddAsync(Guest entity) => GuestDAO.Instance.AddAsync(entity);
    public Task UpdateAsync(Guest entity) => GuestDAO.Instance.UpdateAsync(entity);
    public Task DeleteAsync(Guest entity) => GuestDAO.Instance.DeleteAsync(entity);
    public Task<Guest?> FindExactAsync(string idOrPhone) => GuestDAO.Instance.FindExactAsync(idOrPhone);
}
