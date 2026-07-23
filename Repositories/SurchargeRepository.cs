using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories
{
    public class SurchargeRepository : ISurchargeRepository
    {
        public Task<List<SurchargeItem>> GetActiveItemsAsync() => SurchargeDAO.Instance.GetActiveItemsAsync();

        public Task<List<Surcharge>> GetForStayAsync(int stayId) => SurchargeDAO.Instance.GetForStayAsync(stayId);

        public Task<Dictionary<int, decimal>> GetTotalsAsync(IEnumerable<int> stayIds)
            => SurchargeDAO.Instance.GetTotalsAsync(stayIds);

        public Task<(bool Ok, string Message)> AddAsync(int stayId, int surchargeItemId, int quantity, int createdByUserId)
            => SurchargeDAO.Instance.AddAsync(stayId, surchargeItemId, quantity, createdByUserId);

        public Task<(bool Ok, string Message)> RemoveAsync(int surchargeId)
            => SurchargeDAO.Instance.RemoveAsync(surchargeId);
    }
}
