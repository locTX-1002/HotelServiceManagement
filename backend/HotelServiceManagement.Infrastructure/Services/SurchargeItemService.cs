using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Surcharges;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services;

public class SurchargeItemService : ISurchargeItemService
{
    private readonly HotelDbContext _context;

    public SurchargeItemService(HotelDbContext context) => _context = context;

    public async Task<AuthServiceResult<IReadOnlyList<SurchargeItemResponse>>> GetAllAsync()
    {
        var items = await _context.SurchargeItems.AsNoTracking().OrderBy(i => i.Name).ToListAsync();
        return AuthServiceResult<IReadOnlyList<SurchargeItemResponse>>.Success(items.Select(ToResponse).ToList());
    }

    public async Task<AuthServiceResult<SurchargeItemResponse>> GetByIdAsync(int id)
    {
        var item = await _context.SurchargeItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return item == null
            ? AuthServiceResult<SurchargeItemResponse>.Failure("Surcharge item not found.", 404)
            : AuthServiceResult<SurchargeItemResponse>.Success(ToResponse(item));
    }

    public async Task<AuthServiceResult<SurchargeItemResponse>> CreateAsync(SurchargeItemRequest request)
    {
        var validation = Validate(request);
        if (validation != null) return AuthServiceResult<SurchargeItemResponse>.Failure(validation);

        var name = request.Name.Trim();
        if (await _context.SurchargeItems.AnyAsync(i => i.Name.ToLower() == name.ToLower()))
            return AuthServiceResult<SurchargeItemResponse>.Failure("Surcharge item name already exists.", 409);

        var item = new SurchargeItem
        {
            Name = name,
            Unit = request.Unit.Trim(),
            UnitPrice = request.UnitPrice,
            IsActive = request.IsActive
        };
        _context.SurchargeItems.Add(item);
        await _context.SaveChangesAsync();
        return AuthServiceResult<SurchargeItemResponse>.Success(ToResponse(item), "Surcharge item created successfully.");
    }

    public async Task<AuthServiceResult<SurchargeItemResponse>> UpdateAsync(int id, SurchargeItemRequest request)
    {
        var validation = Validate(request);
        if (validation != null) return AuthServiceResult<SurchargeItemResponse>.Failure(validation);

        var item = await _context.SurchargeItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return AuthServiceResult<SurchargeItemResponse>.Failure("Surcharge item not found.", 404);

        var name = request.Name.Trim();
        if (await _context.SurchargeItems.AnyAsync(i => i.Id != id && i.Name.ToLower() == name.ToLower()))
            return AuthServiceResult<SurchargeItemResponse>.Failure("Surcharge item name already exists.", 409);

        item.Name = name;
        item.Unit = request.Unit.Trim();
        item.UnitPrice = request.UnitPrice;
        item.IsActive = request.IsActive;
        await _context.SaveChangesAsync();
        return AuthServiceResult<SurchargeItemResponse>.Success(ToResponse(item), "Surcharge item updated successfully.");
    }

    private static string? Validate(SurchargeItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "Name is required.";
        if (request.Name.Trim().Length > 100) return "Name cannot exceed 100 characters.";
        if (string.IsNullOrWhiteSpace(request.Unit)) return "Unit is required.";
        if (request.Unit.Trim().Length > 20) return "Unit cannot exceed 20 characters.";
        return request.UnitPrice <= 0 ? "UnitPrice must be greater than 0." : null;
    }

    private static SurchargeItemResponse ToResponse(SurchargeItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Unit = item.Unit,
        UnitPrice = item.UnitPrice,
        IsActive = item.IsActive
    };
}
