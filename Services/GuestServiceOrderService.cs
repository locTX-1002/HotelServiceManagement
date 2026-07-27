using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;

namespace Services;

/// <summary>
/// Luong goi dich vu tu Guest Portal. Tach khoi ServiceOrderService cua nhan vien de khong
/// pha quyen service.order.create/process va giam conflict voi workspace ServiceStaff.
/// </summary>
public sealed class GuestServiceOrderService : IGuestServiceOrderService
{
    private readonly IStayRepository _stays;
    private readonly IServiceOrderRepository _orders;
    private readonly IServiceCatalogRepository _catalog;

    public GuestServiceOrderService()
        : this(new StayRepository(), new ServiceOrderRepository(), new ServiceCatalogRepository()) { }

    public GuestServiceOrderService(
        IStayRepository stays,
        IServiceOrderRepository orders,
        IServiceCatalogRepository catalog)
    {
        _stays = stays;
        _orders = orders;
        _catalog = catalog;
    }

    /// <summary>
    /// Resolve Stay tu CurrentGuestId. UI khong truyen GuestId/StayId nen guest A khong the
    /// sua binding de tao don cho phong cua guest B.
    /// </summary>
    public async Task<ServiceResult<Stay>> GetCurrentActiveStayAsync()
    {
        var guestId = AppSession.CurrentGuestId;
        if (!guestId.HasValue)
            return ServiceResult<Stay>.Failure("Bạn cần đăng nhập tài khoản khách hàng.");

        var active = await _stays.GetActiveAsync();
        var stay = active
            .Where(x => x.Reservation.GuestId == guestId.Value)
            .OrderByDescending(x => x.ActualCheckIn)
            .FirstOrDefault();

        return stay == null
            ? ServiceResult<Stay>.Failure("Bạn chỉ có thể gọi dịch vụ khi đang nhận phòng tại khách sạn.")
            : ServiceResult<Stay>.Success(stay);
    }

    public async Task<ServiceResult<List<ServiceOrder>>> GetCurrentStayOrdersAsync()
    {
        var stay = await GetCurrentActiveStayAsync();
        if (!stay.Ok || stay.Data == null)
            return ServiceResult<List<ServiceOrder>>.Failure(stay.Message);

        var orders = await _orders.GetByStayAsync(stay.Data.Id);
        return ServiceResult<List<ServiceOrder>>.Success(orders);
    }

    public async Task<ServiceResult<ServiceOrder>> CreateAsync(IReadOnlyCollection<ServiceOrderLine> lines)
    {
        var stayResult = await GetCurrentActiveStayAsync();
        if (!stayResult.Ok || stayResult.Data == null)
            return ServiceResult<ServiceOrder>.Failure(stayResult.Message);

        if (lines.Count == 0 || lines.Any(x => x.Quantity <= 0))
            return ServiceResult<ServiceOrder>.Failure("Đơn dịch vụ không hợp lệ.");

        // Re-check bang repository ngay truoc khi ghi, tranh Stay vua check-out sau luc load UI.
        if (!await _orders.IsStayActiveAsync(stayResult.Data.Id))
            return ServiceResult<ServiceOrder>.Failure("Lượt lưu trú đã kết thúc, không thể gọi thêm dịch vụ.");

        var items = await _catalog.GetItemsAsync(availableOnly: true);
        var map = items.ToDictionary(x => x.Id);
        if (lines.Any(x => !map.ContainsKey(x.ServiceItemId)))
            return ServiceResult<ServiceOrder>.Failure("Có dịch vụ không tồn tại hoặc đã ngừng bán.");

        var order = new ServiceOrder
        {
            StayId = stayResult.Data.Id,
            OrderDate = DateTime.Now,
            Status = ServiceOrderStatus.Pending,
            // Null co nghia don khong duoc tao boi nhan vien; guest duoc suy ra qua Stay -> Reservation -> Guest.
            CreatedByUserId = null
        };

        foreach (var line in lines)
        {
            var item = map[line.ServiceItemId];
            order.OrderDetails.Add(new ServiceOrderDetail
            {
                ServiceItemId = item.Id,
                Quantity = line.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.UnitPrice * line.Quantity
            });
        }

        order.TotalAmount = order.OrderDetails.Sum(x => x.Subtotal);
        await _orders.AddAsync(order);
        return ServiceResult<ServiceOrder>.Success(order, "Đã gửi yêu cầu dịch vụ tới nhân viên phục vụ.");
    }
}
