using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.ServiceOrders;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class ServiceOrderService : IServiceOrderService
    {
        private readonly HotelDbContext _context;

        public ServiceOrderService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<AuthServiceResult<IReadOnlyList<ServiceOrderResponse>>> GetAllAsync()
        {
            var orders = await QueryOrders()
                .OrderByDescending(o => o.OrderDate)
                .ThenBy(o => o.Id)
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<ServiceOrderResponse>>.Success(orders.Select(ToResponse).ToList());
        }

        public async Task<AuthServiceResult<ServiceOrderResponse>> GetByIdAsync(int id)
        {
            var order = await QueryOrders().FirstOrDefaultAsync(o => o.Id == id);
            return order == null
                ? AuthServiceResult<ServiceOrderResponse>.Failure("Service order not found.", 404)
                : AuthServiceResult<ServiceOrderResponse>.Success(ToResponse(order));
        }

        public async Task<AuthServiceResult<ServiceOrderResponse>> CreateAsync(CreateServiceOrderRequest request)
        {
            if (request == null)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure("Request body is required.");
            }

            if (request.StayId <= 0)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure("StayId is required.");
            }

            if (request.Details == null || request.Details.Count == 0)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure("At least one service order detail is required.");
            }

            var stayExists = await _context.Stays.AnyAsync(s => s.Id == request.StayId && s.Status == StayStatus.Active);
            if (!stayExists)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure("Active stay does not exist.");
            }

            var serviceItemIds = request.Details.Select(d => d.ServiceItemId).Distinct().ToList();
            var serviceItems = await _context.ServiceItems
                .Where(i => serviceItemIds.Contains(i.Id) && i.IsAvailable)
                .ToListAsync();

            if (serviceItems.Count != serviceItemIds.Count)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure("One or more service items do not exist or are not available.");
            }

            var order = new ServiceOrder
            {
                StayId = request.StayId,
                OrderDate = DateTime.UtcNow,
                Status = ServiceOrderStatus.Pending,
                TotalAmount = 0
            };

            foreach (var detailRequest in request.Details)
            {
                if (detailRequest.Quantity <= 0)
                {
                    return AuthServiceResult<ServiceOrderResponse>.Failure("Quantity must be greater than 0.");
                }

                var serviceItem = serviceItems.First(i => i.Id == detailRequest.ServiceItemId);
                var subtotal = serviceItem.UnitPrice * detailRequest.Quantity;
                order.OrderDetails.Add(new ServiceOrderDetail
                {
                    ServiceItemId = serviceItem.Id,
                    Quantity = detailRequest.Quantity,
                    UnitPrice = serviceItem.UnitPrice,
                    Subtotal = subtotal
                });
                order.TotalAmount += subtotal;
            }

            _context.ServiceOrders.Add(order);
            await _context.SaveChangesAsync();

            var savedOrder = await QueryOrders().FirstAsync(o => o.Id == order.Id);
            return AuthServiceResult<ServiceOrderResponse>.Success(ToResponse(savedOrder), "Service order created successfully.");
        }

        public async Task<AuthServiceResult<ServiceOrderResponse>> UpdateStatusAsync(int id, UpdateServiceOrderStatusRequest request)
        {
            var order = await QueryOrders().FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure("Service order not found.", 404);
            }

            order.Status = request.Status;
            await _context.SaveChangesAsync();

            return AuthServiceResult<ServiceOrderResponse>.Success(ToResponse(order), "Service order status updated successfully.");
        }

        private IQueryable<ServiceOrder> QueryOrders()
        {
            return _context.ServiceOrders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ServiceItem);
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
