using BusinessObjects.Entities;

namespace Repositories
{
    public interface IGuestRepository
    {
        Task<List<Guest>> SearchAsync(string? keyword);
        Task<Guest?> GetByIdAsync(int id);
        Task<bool> IdentityExistsAsync(string identityNumber, int? excludeId = null);
        Task<Guest> AddAsync(Guest guest);
        Task UpdateAsync(Guest guest);
    }
}
