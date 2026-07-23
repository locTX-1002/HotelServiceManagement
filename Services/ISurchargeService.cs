using BusinessObjects.Entities;
namespace Services;

public interface ISurchargeService { Task<List<SurchargeItem>> GetItemsAsync(); Task<ServiceResult<SurchargeItem>> SaveItemAsync(int? id, string name, string unit, decimal price, bool active); Task<List<Surcharge>> GetByStayAsync(int id); Task<ServiceResult<Surcharge>> AddToStayAsync(int stayId, int itemId, int quantity); Task<ServiceResult<Surcharge>> UpdateAsync(int id, int quantity); Task<ServiceResult> DeleteAsync(int id);  Task<Dictionary<int, decimal>> GetTotalsAsync(IEnumerable<int> stayIds); }
