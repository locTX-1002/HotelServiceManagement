using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;
namespace Services;

public sealed class ServiceOrderService : IServiceOrderService
{
    private readonly IServiceOrderRepository _orders; private readonly IServiceCatalogRepository _catalog; public ServiceOrderService() : this(new ServiceOrderRepository(), new ServiceCatalogRepository()) { }
    public ServiceOrderService(IServiceOrderRepository o, IServiceCatalogRepository c) { _orders = o; _catalog = c; }
    public Task<List<ServiceOrder>> GetByStayAsync(int id) => _orders.GetByStayAsync(id);
    public async Task<ServiceResult<ServiceOrder>> CreateAsync(int stayId, IReadOnlyCollection<ServiceOrderLine> lines)
    { if (!AuthorizationPolicy.CanCreateServiceOrder) return ServiceResult<ServiceOrder>.Failure("Bạn không có quyền tạo đơn dịch vụ."); if (lines.Count == 0 || lines.Any(x => x.Quantity <= 0)) return ServiceResult<ServiceOrder>.Failure("Đơn dịch vụ không hợp lệ."); if (!await _orders.IsStayActiveAsync(stayId)) return ServiceResult<ServiceOrder>.Failure("Chỉ phòng đang có khách ở mới gọi dịch vụ được."); var items = await _catalog.GetItemsAsync(true); var map = items.ToDictionary(x => x.Id); if (lines.Any(x => !map.ContainsKey(x.ServiceItemId))) return ServiceResult<ServiceOrder>.Failure("Có dịch vụ không tồn tại hoặc đã ngừng bán."); var x = new ServiceOrder { StayId = stayId, OrderDate = DateTime.Now, Status = ServiceOrderStatus.Pending, CreatedByUserId = AppSession.CurrentUser?.Id }; foreach (var line in lines) { var item = map[line.ServiceItemId]; x.OrderDetails.Add(new ServiceOrderDetail { ServiceItemId = item.Id, Quantity = line.Quantity, UnitPrice = item.UnitPrice, Subtotal = item.UnitPrice * line.Quantity }); } x.TotalAmount = x.OrderDetails.Sum(d => d.Subtotal); await _orders.AddAsync(x); return ServiceResult<ServiceOrder>.Success(x, "Đã tạo đơn dịch vụ."); }
    public async Task<ServiceResult<ServiceOrder>> ChangeStatusAsync(int id, ServiceOrderStatus status) { if (!AuthorizationPolicy.CanProcessServiceOrder) return ServiceResult<ServiceOrder>.Failure("Bạn không có quyền xử lý đơn dịch vụ."); if (!Enum.IsDefined(status)) return ServiceResult<ServiceOrder>.Failure("Trạng thái không hợp lệ."); var current = await _orders.GetByIdAsync(id); if (current == null) return ServiceResult<ServiceOrder>.Failure("Không tìm thấy đơn dịch vụ."); // Cho phep Cho lam -> Hoan tat thang, khong bat qua "Dang lam". Khach san co quy mo
        // nay goi mon xong la giao ngay; bat le tan bam hai lan chi de danh dau mot buoc
        // trung gian khong ai theo doi thi chi lam cham viec. Trang thai Dang lam van con
        // cho ai muon dung, chi khong con bat buoc.
        var allowed = current.Status switch { ServiceOrderStatus.Pending => status is ServiceOrderStatus.Processing or ServiceOrderStatus.Completed or ServiceOrderStatus.Cancelled, ServiceOrderStatus.Processing => status is ServiceOrderStatus.Completed or ServiceOrderStatus.Cancelled, _ => false }; if (!allowed) return ServiceResult<ServiceOrder>.Failure("Không chuyển sang trạng thái này được."); var x = await _orders.ChangeStatusAsync(id, status); return x == null ? ServiceResult<ServiceOrder>.Failure("Không tìm thấy đơn dịch vụ.") : ServiceResult<ServiceOrder>.Success(x, "Đã cập nhật trạng thái."); }
}
