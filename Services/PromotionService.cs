using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Microsoft.EntityFrameworkCore;
using Repositories;
namespace Services;

public sealed class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _r; public PromotionService() : this(new PromotionRepository()) { }
    public PromotionService(IPromotionRepository r) => _r = r;
    public Task<List<Promotion>> GetAllAsync() => _r.GetAllAsync();
    public async Task<ServiceResult<Promotion>> SaveAsync(int? id, string code, string? description, PromotionType type, decimal value, DateTime start, DateTime end, bool active)
    {
        if (AppSession.RoleName is not ("Admin" or "Manager")) return ServiceResult<Promotion>.Failure("Ban khong co quyen quan ly khuyen mai.");
        var normalized = code.Trim().ToUpperInvariant(); if (normalized.Length is 0 or > 30) return ServiceResult<Promotion>.Failure("Ma khuyen mai phai tu 1 den 30 ky tu.");
        if (!Enum.IsDefined(type) || value <= 0 || (type == PromotionType.Percentage && value > 100)) return ServiceResult<Promotion>.Failure("Gia tri khuyen mai khong hop le.");
        if (end.Date < start.Date) return ServiceResult<Promotion>.Failure("Ngay ket thuc phai sau ngay bat dau.");
        if (await _r.CodeExistsAsync(normalized, id)) return ServiceResult<Promotion>.Failure("Ma khuyen mai da ton tai.");
        var x = id.HasValue ? await _r.GetByIdAsync(id.Value) : new Promotion(); if (x == null) return ServiceResult<Promotion>.Failure("Khong tim thay khuyen mai.");
        x.Code = normalized; x.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(); x.Type = type; x.Value = value; x.StartDate = start.Date; x.EndDate = end.Date; x.IsActive = active;
        try { await _r.SaveAsync(x, !id.HasValue); } catch (DbUpdateException) { if (await _r.CodeExistsAsync(normalized, id)) return ServiceResult<Promotion>.Failure("Ma khuyen mai da ton tai."); throw; }
        return ServiceResult<Promotion>.Success(x, "Da luu khuyen mai.");
    }
}
