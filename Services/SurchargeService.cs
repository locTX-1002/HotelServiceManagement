using BusinessObjects.Entities;
using Repositories;
namespace Services;

public sealed class SurchargeService : ISurchargeService
{
    private readonly ISurchargeRepository _r; public SurchargeService() : this(new SurchargeRepository()) { }
    public SurchargeService(ISurchargeRepository r) => _r = r; public Task<List<SurchargeItem>> GetItemsAsync() => _r.GetItemsAsync(); public Task<List<Surcharge>> GetByStayAsync(int id) => _r.GetByStayAsync(id);
    public async Task<ServiceResult<SurchargeItem>> SaveItemAsync(int? id, string name, string unit, decimal price, bool active) { if (AppSession.RoleName is not ("Admin" or "Manager")) return ServiceResult<SurchargeItem>.Failure("Bạn không có quyền quản lý phụ thu."); if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100 || string.IsNullOrWhiteSpace(unit) || unit.Trim().Length > 20 || price <= 0) return ServiceResult<SurchargeItem>.Failure("Thông tin phụ thu không hợp lệ."); var x = id.HasValue ? await _r.GetItemAsync(id.Value) : new SurchargeItem(); if (x == null) return ServiceResult<SurchargeItem>.Failure("Không tìm thấy phụ thu."); x.Name = name.Trim(); x.Unit = unit.Trim(); x.UnitPrice = price; x.IsActive = active; await _r.SaveItemAsync(x, !id.HasValue); return ServiceResult<SurchargeItem>.Success(x, "Đã lưu phụ thu."); }
    public async Task<ServiceResult<Surcharge>> AddToStayAsync(int stayId, int itemId, int quantity) { if (AppSession.RoleName is not ("Admin" or "Manager" or "Receptionist")) return ServiceResult<Surcharge>.Failure("Bạn không có quyền thêm phụ thu."); if (quantity <= 0) return ServiceResult<Surcharge>.Failure("Số lượng phải lớn hơn 0."); var x = await _r.AddToStayAsync(stayId, itemId, quantity, AppSession.CurrentUser?.Id); return x == null ? ServiceResult<Surcharge>.Failure("Lượt lưu trú không hoạt động hoặc phụ thu không hợp lệ.") : ServiceResult<Surcharge>.Success(x, "Đã thêm phụ thu."); }
    public async Task<ServiceResult<Surcharge>> UpdateAsync(int id, int quantity) { if (AppSession.RoleName is not ("Admin" or "Manager" or "Receptionist")) return ServiceResult<Surcharge>.Failure("Bạn không có quyền sửa phụ thu."); if (quantity <= 0) return ServiceResult<Surcharge>.Failure("Số lượng phải lớn hơn 0."); var x = await _r.UpdateAsync(id, quantity); return x == null ? ServiceResult<Surcharge>.Failure("Không sửa được phụ thu sau khi đã thanh toán hoặc lượt lưu trú đã đóng.") : ServiceResult<Surcharge>.Success(x, "Đã cập nhật phụ thu."); }
    public async Task<ServiceResult> DeleteAsync(int id) { if (AppSession.RoleName is not ("Admin" or "Manager" or "Receptionist")) return ServiceResult.Failure("Bạn không có quyền xoá phụ thu."); return await _r.DeleteAsync(id) ? ServiceResult.Success("Đã xoá phụ thu.") : ServiceResult.Failure("Không xoá được phụ thu sau khi đã thanh toán hoặc lượt lưu trú đã đóng."); }

    public Task<Dictionary<int, decimal>> GetTotalsAsync(IEnumerable<int> stayIds)
        => _r.GetTotalsAsync(stayIds);
}
