using BusinessObjects.Entities;

namespace Repositories;

public interface IGuestRepository
{
    Task<List<Guest>> GetAllAsync();
    Task<Guest?> GetByIdAsync(int id);
    Task<List<Guest>> SearchAsync(string keyword);
    Task<bool> IdentityNumberExistsAsync(string value, int? excludeId = null);
    Task<bool> HasReservationsAsync(int guestId);
    Task AddAsync(Guest entity);
    Task UpdateAsync(Guest entity);
    Task DeleteAsync(Guest entity);
    Task<Guest?> FindExactAsync(string idOrPhone);
}
