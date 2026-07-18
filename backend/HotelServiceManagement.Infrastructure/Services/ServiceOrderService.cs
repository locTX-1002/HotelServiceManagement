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

            return AuthServiceResult<IReadOnlyList<ServiceOrderResponse>>.Success(
                orders.Select(ToResponse).ToList());
        }

        public async Task<AuthServiceResult<ServiceOrderResponse>> GetByIdAsync(int id)
        {
            if (id <= 0)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Service order id must be greater than 0.");
            }

            var order = await QueryOrders().FirstOrDefaultAsync(o => o.Id == id);

            return order == null
                ? AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Service order not found.", 404)
                : AuthServiceResult<ServiceOrderResponse>.Success(ToResponse(order));
        }

        /// <summary>
        /// Creates an order only for an active stay. Details must be valid, unique,
        /// available and belong to active service categories.
        /// </summary>
        public async Task<AuthServiceResult<ServiceOrderResponse>> CreateAsync(
            CreateServiceOrderRequest request,
            int? createdByUserId)
        {
            if (request == null)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Request body is required.");
            }

            if (createdByUserId is <= 0)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Authenticated user id is invalid.", 401);
            }

            if (request.StayId <= 0)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "StayId must be greater than 0.");
            }

            if (request.Details == null || request.Details.Count == 0)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "At least one service order detail is required.");
            }

            if (request.Details.Any(d => d == null))
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Service order details cannot contain null items.");
            }

            if (request.Details.Any(d => d.ServiceItemId <= 0))
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Every ServiceItemId must be greater than 0.");
            }

            if (request.Details.Any(d => d.Quantity <= 0))
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Every quantity must be greater than 0.");
            }

            var hasDuplicateServiceItems = request.Details
                .GroupBy(d => d.ServiceItemId)
                .Any(g => g.Count() > 1);

            if (hasDuplicateServiceItems)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "A service item cannot appear more than once in the same order.");
            }

            var stayExists = await _context.Stays.AnyAsync(s =>
                s.Id == request.StayId
                && s.Status == StayStatus.Active);

            if (!stayExists)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Active stay does not exist.", 404);
            }

            var serviceItemIds = request.Details
                .Select(d => d.ServiceItemId)
                .ToList();

            var serviceItems = await _context.ServiceItems
                .Include(i => i.ServiceCategory)
                .Where(i =>
                    serviceItemIds.Contains(i.Id)
                    && i.IsAvailable
                    && i.ServiceCategory.IsActive)
                .ToListAsync();

            if (serviceItems.Count != serviceItemIds.Count)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "One or more service items do not exist, are unavailable, or belong to an inactive category.");
            }

            var serviceItemById = serviceItems.ToDictionary(i => i.Id);
            var order = new ServiceOrder
            {
                StayId = request.StayId,
                OrderDate = DateTime.UtcNow,
                Status = ServiceOrderStatus.Pending,
                TotalAmount = 0,
                CreatedByUserId = createdByUserId
            };

            foreach (var detailRequest in request.Details)
            {
                var serviceItem = serviceItemById[detailRequest.ServiceItemId];
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

            var savedOrder = await QueryOrders()
                .FirstAsync(o => o.Id == order.Id);

            return AuthServiceResult<ServiceOrderResponse>.Success(
                ToResponse(savedOrder),
                "Service order created successfully.");
        }

        /// <summary>
        /// Enforces Pending -> Processing/Cancelled and
        /// Processing -> Completed/Cancelled. Completed/Cancelled are terminal.
        /// </summary>
        public async Task<AuthServiceResult<ServiceOrderResponse>> UpdateStatusAsync(
            int id,
            UpdateServiceOrderStatusRequest request)
        {
            if (id <= 0)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Service order id must be greater than 0.");
            }

            if (request == null)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Request body is required.");
            }

            if (!Enum.IsDefined(typeof(ServiceOrderStatus), request.Status))
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Service order status is invalid.");
            }

            var order = await QueryOrders().FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    "Service order not found.", 404);
            }

            if (order.Status == request.Status)
            {
                return AuthServiceResult<ServiceOrderResponse>.Success(
                    ToResponse(order),
                    "Service order status is unchanged.");
            }

            if (!IsAllowedStatusTransition(order.Status, request.Status))
            {
                return AuthServiceResult<ServiceOrderResponse>.Failure(
                    $"Invalid service order status transition from {order.Status} to {request.Status}.",
                    409);
            }

            // New work cannot start or finish after the guest has checked out.
            if (request.Status == ServiceOrderStatus.Processing
                || request.Status == ServiceOrderStatus.Completed)
            {
                var stayIsActive = await _context.Stays.AnyAsync(s =>
                    s.Id == order.StayId
                    && s.Status == StayStatus.Active);

                if (!stayIsActive)
                {
                    return AuthServiceResult<ServiceOrderResponse>.Failure(
                        "Service order cannot be processed or completed after the stay is closed.",
                        409);
                }
            }

            order.Status = request.Status;
            await _context.SaveChangesAsync();

            return AuthServiceResult<ServiceOrderResponse>.Success(
                ToResponse(order),
                "Service order status updated successfully.");
        }

        private static bool IsAllowedStatusTransition(
            ServiceOrderStatus currentStatus,
            ServiceOrderStatus newStatus)
        {
            return currentStatus switch
            {
                ServiceOrderStatus.Pending =>
                    newStatus == ServiceOrderStatus.Processing
                    || newStatus == ServiceOrderStatus.Cancelled,

                ServiceOrderStatus.Processing =>
                    newStatus == ServiceOrderStatus.Completed
                    || newStatus == ServiceOrderStatus.Cancelled,

                ServiceOrderStatus.Completed => false,
                ServiceOrderStatus.Cancelled => false,
                _ => false
            };
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