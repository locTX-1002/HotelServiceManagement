using BusinessObjects.Entities;
using Repositories;
namespace Services;

public sealed class ServiceCatalogService : IServiceCatalogService
{
    private readonly IServiceCatalogRepository _r; public ServiceCatalogService() : this(new ServiceCatalogRepository()) { }
    public ServiceCatalogService(IServiceCatalogRepository r) => _r = r;
    public Task<List<ServiceCategory>> GetCategoriesAsync() => _r.GetCategoriesAsync(); public Task<List<ServiceItem>> GetItemsAsync(bool only = false) => _r.GetItemsAsync(only);
    public async Task<ServiceResult<ServiceCategory>> SaveCategoryAsync(int? id, string name, bool active)
    { if (!AuthorizationPolicy.CanManageServiceCatalog) return ServiceResult<ServiceCategory>.Failure("Chỉ Quản trị viên hoặc Quản lý được quản lý danh mục dịch vụ."); if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100) return ServiceResult<ServiceCategory>.Failure("Tên danh mục không hợp lệ."); var x = id.HasValue ? await _r.GetCategoryAsync(id.Value) : new ServiceCategory(); if (x == null) return ServiceResult<ServiceCategory>.Failure("Không tìm thấy danh mục."); x.CategoryName = name.Trim(); x.IsActive = active; await _r.SaveCategoryAsync(x, !id.HasValue); return ServiceResult<ServiceCategory>.Success(x, "Đã lưu danh mục."); }
    public async Task<ServiceResult<ServiceItem>> SaveItemAsync(int? id, int categoryId, string name, decimal price, bool available)
    { if (!AuthorizationPolicy.CanManageServiceCatalog) return ServiceResult<ServiceItem>.Failure("Chỉ Quản trị viên hoặc Quản lý được quản lý dịch vụ."); if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100) return ServiceResult<ServiceItem>.Failure("Tên dịch vụ không hợp lệ."); if (price < 0) return ServiceResult<ServiceItem>.Failure("Đơn giá không được âm."); var cat = await _r.GetCategoryAsync(categoryId); if (cat == null) return ServiceResult<ServiceItem>.Failure("Không tìm thấy danh mục."); var x = id.HasValue ? await _r.GetItemAsync(id.Value) : new ServiceItem(); if (x == null) return ServiceResult<ServiceItem>.Failure("Không tìm thấy dịch vụ."); x.ServiceCategoryId = categoryId; x.ServiceName = name.Trim(); x.UnitPrice = price; x.IsAvailable = available; await _r.SaveItemAsync(x, !id.HasValue); x.ServiceCategory = cat; return ServiceResult<ServiceItem>.Success(x, "Đã lưu dịch vụ."); }
}
