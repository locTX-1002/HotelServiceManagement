using BusinessObjects.Entities;
using DataAccessObjects;
namespace Repositories;

public sealed class SurchargeRepository : ISurchargeRepository { public Task<List<SurchargeItem>> GetItemsAsync() => SurchargeDAO.Instance.GetItemsAsync(); public Task<SurchargeItem?> GetItemAsync(int id) => SurchargeDAO.Instance.GetItemAsync(id); public Task SaveItemAsync(SurchargeItem x, bool add) => SurchargeDAO.Instance.SaveItemAsync(x, add); public Task<List<Surcharge>> GetByStayAsync(int id) => SurchargeDAO.Instance.GetByStayAsync(id); public Task<Surcharge?> AddToStayAsync(int s, int i, int q, int? u) => SurchargeDAO.Instance.AddToStayAsync(s, i, q, u); public Task<Surcharge?> UpdateAsync(int id, int quantity) => SurchargeDAO.Instance.UpdateAsync(id, quantity); public Task<bool> DeleteAsync(int id) => SurchargeDAO.Instance.DeleteAsync(id);     public Task<Dictionary<int, decimal>> GetTotalsAsync(IEnumerable<int> stayIds)
        => SurchargeDAO.Instance.GetTotalsAsync(stayIds);
}
