using BusinessObjects.Entities;
namespace Repositories;

public interface IPromotionRepository
{
    Task<List<Promotion>> GetAllAsync(); Task<Promotion?> GetByIdAsync(int id); Task<Promotion?> GetByCodeAsync(string code);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null); Task SaveAsync(Promotion entity, bool add);
}
