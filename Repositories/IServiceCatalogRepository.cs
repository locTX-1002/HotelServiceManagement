using BusinessObjects.Entities;
namespace Repositories;
public interface IServiceCatalogRepository
{
    Task<List<ServiceCategory>> GetCategoriesAsync(); Task<List<ServiceItem>> GetItemsAsync(bool availableOnly = false);
    Task<ServiceCategory?> GetCategoryAsync(int id); Task<ServiceItem?> GetItemAsync(int id);
    Task SaveCategoryAsync(ServiceCategory x, bool add); Task SaveItemAsync(ServiceItem x, bool add);
}
