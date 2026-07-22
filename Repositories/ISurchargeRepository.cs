using BusinessObjects.Entities;
namespace Repositories;

public interface ISurchargeRepository { Task<List<SurchargeItem>> GetItemsAsync(); Task<SurchargeItem?> GetItemAsync(int id); Task SaveItemAsync(SurchargeItem x, bool add); Task<List<Surcharge>> GetByStayAsync(int id); Task<Surcharge?> AddToStayAsync(int stayId, int itemId, int quantity, int? userId); Task<Surcharge?> UpdateAsync(int id, int quantity); Task<bool> DeleteAsync(int id); }
