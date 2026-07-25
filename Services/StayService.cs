using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;

namespace Services;

public sealed class StayService : IStayService
{
    private readonly IStayRepository _stays;
    private readonly IReservationRepository _reservations;

    public StayService() : this(new StayRepository(), new ReservationRepository()) { }
    public StayService(IStayRepository stays, IReservationRepository reservations)
    { _stays = stays; _reservations = reservations; }

    public Task<List<Stay>> GetActiveAsync() => _stays.GetActiveAsync();

    public async Task<ServiceResult<Stay>> CheckInAsync(int reservationId, DateTime? actualCheckIn = null)
    {
        if (!CanOperate()) return ServiceResult<Stay>.Failure("Bạn không có quyền nhận phòng.");
        var reservation = await _reservations.GetByIdAsync(reservationId);
        if (reservation == null) return ServiceResult<Stay>.Failure("Không tìm thấy đặt phòng.");
        if (reservation.Status != ReservationStatus.Confirmed)
            return ServiceResult<Stay>.Failure("Chỉ đặt phòng đã xác nhận mới nhận phòng được.");
        if (string.IsNullOrWhiteSpace(reservation.Guest.IdentityNumber))
            return ServiceResult<Stay>.Failure("Phải xác minh giấy tờ khách hàng trước khi nhận phòng.");
        var time = actualCheckIn ?? DateTime.Now;
        var stay = await _stays.CheckInAsync(reservationId, AppSession.CurrentUser?.Id, time);
        return stay == null
            ? ServiceResult<Stay>.Failure("Không nhận phòng được vì trạng thái phòng hoặc đặt phòng đã thay đổi.")
            : ServiceResult<Stay>.Success(stay, "Nhận phòng thành công.");
    }

    public async Task<ServiceResult<Stay>> CheckOutAsync(int stayId, DateTime? actualCheckOut = null)
    {
        if (!CanOperate()) return ServiceResult<Stay>.Failure("Bạn không có quyền trả phòng.");
        var stay = await _stays.GetByIdAsync(stayId);
        if (stay == null || stay.Status != StayStatus.Active)
            return ServiceResult<Stay>.Failure("Không tìm thấy lượt lưu trú đang hoạt động.");
        if (stay.ServiceOrders.Any(o => o.Status is ServiceOrderStatus.Pending or ServiceOrderStatus.Processing))
            return ServiceResult<Stay>.Failure("Phải hoàn tất hoặc huỷ tất cả đơn dịch vụ trước khi trả phòng.");
        if (stay.Invoice == null || stay.Invoice.Status != InvoiceStatus.Paid)
            return ServiceResult<Stay>.Failure("Hoá đơn phải được thanh toán đầy đủ trước khi trả phòng.");
        var result = await _stays.CheckOutAsync(stayId, AppSession.CurrentUser?.Id,
            actualCheckOut ?? DateTime.Now);
        return result == null
            ? ServiceResult<Stay>.Failure("Không trả phòng được vì trạng thái đã thay đổi.")
            : ServiceResult<Stay>.Success(result, "Trả phòng thành công; phòng chuyển sang Đang dọn.");
    }

    private static bool CanOperate() => AppSession.RoleName is "Admin" or "Manager" or "Receptionist";

    public Task<List<Stay>> GetBillableAsync() => _stays.GetBillableAsync();

    public Task<List<Reservation>> GetArrivalsAsync() => _stays.GetArrivalsAsync();

    public async Task<ServiceResult> ExtendAsync(int stayId, DateTime newCheckOut)
    {
        var (ok, message) = await _stays.ExtendAsync(stayId, newCheckOut);
        return ok ? ServiceResult.Success(message) : ServiceResult.Failure(message);
    }
}
