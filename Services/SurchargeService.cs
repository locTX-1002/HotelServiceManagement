using BusinessObjects.Entities;
using Repositories;
namespace Services;

public sealed class SurchargeService : ISurchargeService
{
    private readonly ISurchargeRepository _r; public SurchargeService() : this(new SurchargeRepository()) { }
    public SurchargeService(ISurchargeRepository r) => _r = r; public Task<List<SurchargeItem>> GetItemsAsync() => _r.GetItemsAsync(); public Task<List<Surcharge>> GetByStayAsync(int id) => _r.GetByStayAsync(id);
    public async Task<ServiceResult<SurchargeItem>> SaveItemAsync(int? id, string name, string unit, decimal price, bool active) { if (AppSession.RoleName is not ("Admin" or "Manager")) return ServiceResult<SurchargeItem>.Failure("Ban khong co quyen quan ly phu thu."); if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100 || string.IsNullOrWhiteSpace(unit) || unit.Trim().Length > 20 || price <= 0) return ServiceResult<SurchargeItem>.Failure("Thong tin phu thu khong hop le."); var x = id.HasValue ? await _r.GetItemAsync(id.Value) : new SurchargeItem(); if (x == null) return ServiceResult<SurchargeItem>.Failure("Khong tim thay phu thu."); x.Name = name.Trim(); x.Unit = unit.Trim(); x.UnitPrice = price; x.IsActive = active; await _r.SaveItemAsync(x, !id.HasValue); return ServiceResult<SurchargeItem>.Success(x, "Da luu phu thu."); }
    public async Task<ServiceResult<Surcharge>> AddToStayAsync(int stayId, int itemId, int quantity) { if (AppSession.RoleName is not ("Admin" or "Manager" or "Receptionist")) return ServiceResult<Surcharge>.Failure("Ban khong co quyen them phu thu."); if (quantity <= 0) return ServiceResult<Surcharge>.Failure("So luong phai lon hon 0."); var x = await _r.AddToStayAsync(stayId, itemId, quantity, AppSession.CurrentUser?.Id); return x == null ? ServiceResult<Surcharge>.Failure("Stay khong hoat dong hoac phu thu khong hop le.") : ServiceResult<Surcharge>.Success(x, "Da them phu thu."); }
    public async Task<ServiceResult<Surcharge>> UpdateAsync(int id, int quantity) { if (AppSession.RoleName is not ("Admin" or "Manager" or "Receptionist")) return ServiceResult<Surcharge>.Failure("Ban khong co quyen sua phu thu."); if (quantity <= 0) return ServiceResult<Surcharge>.Failure("So luong phai lon hon 0."); var x = await _r.UpdateAsync(id, quantity); return x == null ? ServiceResult<Surcharge>.Failure("Khong the sua phu thu sau khi thanh toan hoac stay da dong.") : ServiceResult<Surcharge>.Success(x, "Da cap nhat phu thu."); }
    public async Task<ServiceResult> DeleteAsync(int id) { if (AppSession.RoleName is not ("Admin" or "Manager" or "Receptionist")) return ServiceResult.Failure("Ban khong co quyen xoa phu thu."); return await _r.DeleteAsync(id) ? ServiceResult.Success("Da xoa phu thu.") : ServiceResult.Failure("Khong the xoa phu thu sau khi thanh toan hoac stay da dong."); }

    public Task<Dictionary<int, decimal>> GetTotalsAsync(IEnumerable<int> stayIds)
        => _r.GetTotalsAsync(stayIds);
}
