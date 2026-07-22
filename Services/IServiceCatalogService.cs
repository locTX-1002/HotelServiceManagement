using BusinessObjects.Entities;
namespace Services;
public interface IServiceCatalogService
{
    Task<List<ServiceCategory>> GetCategoriesAsync(); Task<List<ServiceItem>> GetItemsAsync(bool onlyAvailable=false);
    Task<ServiceResult<ServiceCategory>> SaveCategoryAsync(int? id,string name,bool active); Task<ServiceResult<ServiceItem>> SaveItemAsync(int? id,int categoryId,string name,decimal price,bool available);
}
