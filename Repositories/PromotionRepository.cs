using BusinessObjects.Entities;
using DataAccessObjects;
namespace Repositories;

public sealed class PromotionRepository : IPromotionRepository
{
    public Task<List<Promotion>> GetAllAsync() => PromotionDAO.Instance.GetAllAsync(); public Task<Promotion?> GetByIdAsync(int id) => PromotionDAO.Instance.GetByIdAsync(id);
    public Task<Promotion?> GetByCodeAsync(string code) => PromotionDAO.Instance.GetByCodeAsync(code); public Task<bool> CodeExistsAsync(string code, int? id = null) => PromotionDAO.Instance.CodeExistsAsync(code, id);
    public Task SaveAsync(Promotion x, bool add) => PromotionDAO.Instance.SaveAsync(x, add);
}
