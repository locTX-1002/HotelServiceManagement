using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceItems;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class ServiceItemService : IServiceItemService
    {
        private readonly HotelDbContext _context;

        public ServiceItemService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<AuthServiceResult<IReadOnlyList<ServiceItemResponse>>> GetAllAsync()
        {
            var items = await QueryItems()
                .OrderBy(i => i.ServiceCategory.CategoryName)
                .ThenBy(i => i.ServiceName)
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<ServiceItemResponse>>.Success(items.Select(ToResponse).ToList());
        }

        public async Task<AuthServiceResult<ServiceItemResponse>> GetByIdAsync(int id)
        {
            var item = await QueryItems().FirstOrDefaultAsync(i => i.Id == id);
            return item == null
                ? AuthServiceResult<ServiceItemResponse>.Failure("Service item not found.", 404)
                : AuthServiceResult<ServiceItemResponse>.Success(ToResponse(item));
        }

        public async Task<AuthServiceResult<ServiceItemResponse>> CreateAsync(CreateServiceItemRequest request)
        {
            var validationMessage = Validate(request.ServiceCategoryId, request.ServiceName, request.UnitPrice);
            if (validationMessage != null)
            {
                return AuthServiceResult<ServiceItemResponse>.Failure(validationMessage);
            }

            var category = await _context.ServiceCategories.FirstOrDefaultAsync(c => c.Id == request.ServiceCategoryId && c.IsActive);
            if (category == null)
            {
                return AuthServiceResult<ServiceItemResponse>.Failure("Active service category does not exist.");
            }

            var serviceName = request.ServiceName.Trim();
            var exists = await _context.ServiceItems.AnyAsync(i => i.ServiceName.ToLower() == serviceName.ToLower());
            if (exists)
            {
                return AuthServiceResult<ServiceItemResponse>.Failure("Service item name already exists.", 409);
            }

            var item = new ServiceItem
            {
                ServiceCategoryId = request.ServiceCategoryId,
                ServiceName = serviceName,
                UnitPrice = request.UnitPrice,
                IsAvailable = request.IsAvailable
            };

            _context.ServiceItems.Add(item);
            await _context.SaveChangesAsync();
            item.ServiceCategory = category;

            return AuthServiceResult<ServiceItemResponse>.Success(ToResponse(item), "Service item created successfully.");
        }

        public async Task<AuthServiceResult<ServiceItemResponse>> UpdateAsync(int id, UpdateServiceItemRequest request)
        {
            var validationMessage = Validate(request.ServiceCategoryId, request.ServiceName, request.UnitPrice);
            if (validationMessage != null)
            {
                return AuthServiceResult<ServiceItemResponse>.Failure(validationMessage);
            }

            var item = await QueryItems().FirstOrDefaultAsync(i => i.Id == id);
            if (item == null)
            {
                return AuthServiceResult<ServiceItemResponse>.Failure("Service item not found.", 404);
            }

            var category = await _context.ServiceCategories.FirstOrDefaultAsync(c => c.Id == request.ServiceCategoryId && c.IsActive);
            if (category == null)
            {
                return AuthServiceResult<ServiceItemResponse>.Failure("Active service category does not exist.");
            }

            var serviceName = request.ServiceName.Trim();
            var exists = await _context.ServiceItems.AnyAsync(i => i.Id != id && i.ServiceName.ToLower() == serviceName.ToLower());
            if (exists)
            {
                return AuthServiceResult<ServiceItemResponse>.Failure("Service item name already exists.", 409);
            }

            item.ServiceCategoryId = request.ServiceCategoryId;
            item.ServiceName = serviceName;
            item.UnitPrice = request.UnitPrice;
            item.IsAvailable = request.IsAvailable;
            item.ServiceCategory = category;

            await _context.SaveChangesAsync();

            return AuthServiceResult<ServiceItemResponse>.Success(ToResponse(item), "Service item updated successfully.");
        }

        private IQueryable<ServiceItem> QueryItems()
        {
            return _context.ServiceItems.Include(i => i.ServiceCategory);
        }

        private static ServiceItemResponse ToResponse(ServiceItem item)
        {
            return new ServiceItemResponse
            {
                Id = item.Id,
                ServiceCategoryId = item.ServiceCategoryId,
                CategoryName = item.ServiceCategory?.CategoryName ?? string.Empty,
                ServiceName = item.ServiceName,
                UnitPrice = item.UnitPrice,
                IsAvailable = item.IsAvailable
            };
        }

        private static string? Validate(int categoryId, string serviceName, decimal unitPrice)
        {
            if (categoryId <= 0)
            {
                return "ServiceCategoryId is required.";
            }

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return "ServiceName is required.";
            }

            return unitPrice < 0 ? "UnitPrice must be greater than or equal to 0." : null;
        }
    }
}
