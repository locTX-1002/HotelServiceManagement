using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceItems;
using HotelServiceManagement.Application.DTOs.ServiceOrders;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    // Khach tu dat va xem lai dich vu trong luc dang luu tru. GuestId luon lay tu JWT;
    // client khong duoc phep truyen GuestId/StayId de xem du lieu cua nguoi khac.
    public class GuestServiceOrderService : IGuestServiceOrderService
    {
        private readonly HotelDbContext _context;
        private readonly IServiceOrderService _serviceOrderService;

        public GuestServiceOrderService(
            HotelDbContext context,
            IServiceOrderService serviceOrderService)
        {
            _context = context;
            _serviceOrderService = serviceOrderService;
        }

        public async Task<AuthServiceResult<IReadOnlyList<ServiceItemResponse>>> GetCatalogAsync()
        {
            var items = await _context.ServiceItems
                .AsNoTracking()
                .Include(i => i.ServiceCategory)
                .Where(i =>
                    i.IsAvailable
                    && i.ServiceCategory.IsActive)
                .OrderBy(i => i.ServiceCategory.CategoryName)
                .ThenBy(i => i.ServiceName)
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

        public async Task<AuthServiceResult<IReadOnlyList<ServiceOrderResponse>>> GetMyOrdersAsync(
            int guestId)
        {
            if (guestId <= 0)
            {
                return AuthServiceResult<IReadOnlyList<ServiceOrderResponse>>.Failure(
                    "Authenticated guest id is invalid.", 401);
            }

            // The contract in CAN_SUA_BACKEND.md is scoped to the guest's active stay.
            // An empty list is intentional when the guest has not checked in or has checked out.
            var orders = await _context.ServiceOrders
                .AsNoTracking()
                .Include(o => o.CreatedByUser)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ServiceItem)
                .Where(o =>
                    o.Stay.Status == StayStatus.Active
                    && o.Stay.Reservation.GuestId == guestId)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<ServiceOrderResponse>>.Success(
                orders.Select(ToResponse).ToList());
        }

        public async Task<AuthServiceResult<ServiceOrderResponse>> CreateOrderAsync(
            int guestId,
            GuestCreateServiceOrderRequest request)
        {
            if (guestId <= 0)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Authenticated guest id is invalid.", 401);
            }

            if (request == null || request.Items == null || request.Items.Count == 0)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "At least one item is required.");
            }

            var stay = await _context.Stays
                .Where(s =>
                    s.Status == StayStatus.Active
                    && s.Reservation.GuestId == guestId)
                .OrderByDescending(s => s.ActualCheckIn)
                .FirstOrDefaultAsync();

            if (stay == null)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "No active stay found for this guest - ordering services is only available while checked in.",
                    404);
            }

            var createRequest = new CreateServiceOrderRequest
            {
                StayId = stay.Id,
                Details = request.Items
            };

            return await _serviceOrderService.CreateAsync(
                createRequest,
                createdByUserId: null);
        }

        private static ServiceOrderResponse ToResponse(ServiceOrder order)
        {
            return new ServiceOrderResponse
            {
                Id = order.Id,
                StayId = order.StayId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                CreatedByUserId = order.CreatedByUserId,
                CreatedByUserName = order.CreatedByUser?.FullName,
                Details = order.OrderDetails
                    .OrderBy(d => d.Id)
                    .Select(d => new ServiceOrderDetailResponse
                    {
                        Id = d.Id,
                        ServiceItemId = d.ServiceItemId,
                        ServiceName = d.ServiceItem?.ServiceName ?? string.Empty,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        Subtotal = d.Subtotal
                    })
                    .ToList()
            };
        }

    }
}
