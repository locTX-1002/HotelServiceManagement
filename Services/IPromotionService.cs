using BusinessObjects.Entities;
using BusinessObjects.Enums;
namespace Services;

public interface IPromotionService
{
    Task<List<Promotion>> GetAllAsync(); Task<ServiceResult<Promotion>> SaveAsync(int? id, string code, string? description, PromotionType type, decimal value, DateTime start, DateTime end, bool active);
}
