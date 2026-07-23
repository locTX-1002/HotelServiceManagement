using BusinessObjects.Entities;

namespace Services
{
    public interface ISurchargeService
    {
        Task<List<SurchargeItem>> GetActiveItemsAsync();
        Task<List<Surcharge>> GetForStayAsync(int stayId);
        Task<Dictionary<int, decimal>> GetTotalsAsync(IEnumerable<int> stayIds);
        Task<ServiceResult> AddAsync(int stayId, int surchargeItemId, int quantity, int createdByUserId);
        Task<ServiceResult> RemoveAsync(int surchargeId);
    }
}
