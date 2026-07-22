using BusinessObjects.Entities; using Repositories;
namespace Services;
public sealed class ServiceCatalogService : IServiceCatalogService
{
    private readonly IServiceCatalogRepository _r; public ServiceCatalogService():this(new ServiceCatalogRepository()){} public ServiceCatalogService(IServiceCatalogRepository r)=>_r=r;
    public Task<List<ServiceCategory>> GetCategoriesAsync()=>_r.GetCategoriesAsync(); public Task<List<ServiceItem>> GetItemsAsync(bool only=false)=>_r.GetItemsAsync(only);
    public async Task<ServiceResult<ServiceCategory>> SaveCategoryAsync(int? id,string name,bool active)
    { if(string.IsNullOrWhiteSpace(name)||name.Trim().Length>100)return ServiceResult<ServiceCategory>.Failure("Ten danh muc khong hop le."); var x=id.HasValue?await _r.GetCategoryAsync(id.Value):new ServiceCategory(); if(x==null)return ServiceResult<ServiceCategory>.Failure("Khong tim thay danh muc."); x.CategoryName=name.Trim();x.IsActive=active;await _r.SaveCategoryAsync(x,!id.HasValue);return ServiceResult<ServiceCategory>.Success(x,"Da luu danh muc."); }
    public async Task<ServiceResult<ServiceItem>> SaveItemAsync(int? id,int categoryId,string name,decimal price,bool available)
    { if(string.IsNullOrWhiteSpace(name)||name.Trim().Length>100)return ServiceResult<ServiceItem>.Failure("Ten dich vu khong hop le.");if(price<0)return ServiceResult<ServiceItem>.Failure("Don gia khong duoc am.");var cat=await _r.GetCategoryAsync(categoryId);if(cat==null)return ServiceResult<ServiceItem>.Failure("Khong tim thay danh muc.");var x=id.HasValue?await _r.GetItemAsync(id.Value):new ServiceItem();if(x==null)return ServiceResult<ServiceItem>.Failure("Khong tim thay dich vu.");x.ServiceCategoryId=categoryId;x.ServiceName=name.Trim();x.UnitPrice=price;x.IsAvailable=available;await _r.SaveItemAsync(x,!id.HasValue);x.ServiceCategory=cat;return ServiceResult<ServiceItem>.Success(x,"Da luu dich vu."); }
}
