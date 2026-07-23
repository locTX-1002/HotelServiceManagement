using BusinessObjects.Entities;

namespace Repositories
{
    public interface ISurchargeRepository
    {
        Task<List<SurchargeItem>> GetActiveItemsAsync();
        Task<List<Surcharge>> GetForStayAsync(int stayId);
        Task<Dictionary<int, decimal>> GetTotalsAsync(IEnumerable<int> stayIds);
        Task<(bool Ok, string Message)> AddAsync(int stayId, int surchargeItemId, int quantity, int createdByUserId);
        Task<(bool Ok, string Message)> RemoveAsync(int surchargeId);
    }
}
