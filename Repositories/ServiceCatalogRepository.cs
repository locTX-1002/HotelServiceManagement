using BusinessObjects.Entities;
using DataAccessObjects;
namespace Repositories;

public sealed class ServiceCatalogRepository : IServiceCatalogRepository
{
    public Task<List<ServiceCategory>> GetCategoriesAsync() => ServiceCatalogDAO.Instance.GetCategoriesAsync(); public Task<List<ServiceItem>> GetItemsAsync(bool only = false) => ServiceCatalogDAO.Instance.GetItemsAsync(only);
    public Task<ServiceCategory?> GetCategoryAsync(int id) => ServiceCatalogDAO.Instance.GetCategoryAsync(id); public Task<ServiceItem?> GetItemAsync(int id) => ServiceCatalogDAO.Instance.GetItemAsync(id);
    public Task SaveCategoryAsync(ServiceCategory x, bool add) => ServiceCatalogDAO.Instance.SaveCategoryAsync(x, add); public Task SaveItemAsync(ServiceItem x, bool add) => ServiceCatalogDAO.Instance.SaveItemAsync(x, add);
}
