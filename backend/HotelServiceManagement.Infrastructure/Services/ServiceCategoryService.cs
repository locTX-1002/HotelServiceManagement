using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceCategories;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class ServiceCategoryService : IServiceCategoryService
    {
        private readonly HotelDbContext _context;

        public ServiceCategoryService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<AuthServiceResult<IReadOnlyList<ServiceCategoryResponse>>> GetAllAsync()
        {
            var categories = await _context.ServiceCategories
                .OrderBy(c => c.CategoryName)
                .Select(c => new ServiceCategoryResponse
                {
                    Id = c.Id,
                    CategoryName = c.CategoryName,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<ServiceCategoryResponse>>.Success(categories);
        }
    }
}
