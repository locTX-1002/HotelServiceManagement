using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceItems;
using HotelServiceManagement.Application.DTOs.ServiceOrders;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    // Khach tu dat dich vu/do an trong luc dang luu tru - chi hop le khi dang co Stay Active, giong
    // het rang buoc cua HousekeepingRequestService (dat dich vu can dang o phong that su).
    public class GuestServiceOrderService : IGuestServiceOrderService
    {
        private readonly HotelDbContext _context;
        private readonly IServiceOrderService _serviceOrderService;

        public GuestServiceOrderService(HotelDbContext context, IServiceOrderService serviceOrderService)
        {
            _context = context;
            _serviceOrderService = serviceOrderService;
        }

        public async Task<AuthServiceResult<IReadOnlyList<ServiceItemResponse>>> GetCatalogAsync()
        {
            var items = await _context.ServiceItems
                .AsNoTracking()
                .Include(i => i.ServiceCategory)
                .Where(i => i.IsAvailable && i.ServiceCategory.IsActive)
                .OrderBy(i => i.ServiceCategory.CategoryName).ThenBy(i => i.ServiceName)
                .Select(i => new ServiceItemResponse
                {
                    Id = i.Id,
                    ServiceCategoryId = i.ServiceCategoryId,
                    CategoryName = i.ServiceCategory.CategoryName,
                    ServiceName = i.ServiceName,
                    UnitPrice = i.UnitPrice,
                    IsAvailable = i.IsAvailable
                })
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<ServiceItemResponse>>.Success(items);
        }

        public async Task<AuthServiceResult<ServiceOrderResponse>> CreateOrderAsync(int guestId, GuestCreateServiceOrderRequest request)
        {
            if (request == null || request.Items == null || request.Items.Count == 0)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure("At least one item is required.");
            }

            var stay = await _context.Stays
                .Where(s => s.Status == StayStatus.Active && s.Reservation.GuestId == guestId)
                .OrderByDescending(s => s.ActualCheckIn)
                .FirstOrDefaultAsync();

            if (stay == null)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "No active stay found for this guest - ordering services is only available while checked in.", 404);
            }

            var createRequest = new CreateServiceOrderRequest
            {
                StayId = stay.Id,
                Details = request.Items
            };

            return await _serviceOrderService.CreateAsync(createRequest, createdByUserId: null);
        }

        public async Task<AuthServiceResult<IReadOnlyList<ServiceOrderResponse>>> GetOrdersAsync(int guestId)
        {
            var orders = await _context.ServiceOrders
                .AsNoTracking()
                .Include(o => o.OrderDetails).ThenInclude(d => d.ServiceItem)
                .Where(o => o.Stay.Status == StayStatus.Active
                    && o.Stay.Reservation.GuestId == guestId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new ServiceOrderResponse
                {
                    Id = o.Id,
                    StayId = o.StayId,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    Details = o.OrderDetails.OrderBy(d => d.Id).Select(d => new ServiceOrderDetailResponse
                    {
                        Id = d.Id,
                        ServiceItemId = d.ServiceItemId,
                        ServiceName = d.ServiceItem.ServiceName,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        Subtotal = d.Subtotal
                    }).ToList()
                })
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<ServiceOrderResponse>>.Success(orders);
        }
    }
}
