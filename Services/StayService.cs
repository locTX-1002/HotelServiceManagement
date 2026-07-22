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
        if (!CanOperate()) return ServiceResult<Stay>.Failure("Ban khong co quyen check-in.");
        var reservation = await _reservations.GetByIdAsync(reservationId);
        if (reservation == null) return ServiceResult<Stay>.Failure("Khong tim thay dat phong.");
        if (reservation.Status != ReservationStatus.Confirmed)
            return ServiceResult<Stay>.Failure("Chi dat phong da xac nhan moi duoc check-in.");
        if (string.IsNullOrWhiteSpace(reservation.Guest.IdentityNumber))
            return ServiceResult<Stay>.Failure("Phai xac minh giay to khach hang truoc khi check-in.");
        var time = actualCheckIn ?? DateTime.Now;
        var stay = await _stays.CheckInAsync(reservationId, AppSession.CurrentUser?.Id, time);
        return stay == null
            ? ServiceResult<Stay>.Failure("Khong the check-in do trang thai phong/dat phong da thay doi.")
            : ServiceResult<Stay>.Success(stay, "Check-in thanh cong.");
    }

    public async Task<ServiceResult<Stay>> CheckOutAsync(int stayId, DateTime? actualCheckOut = null)
    {
        if (!CanOperate()) return ServiceResult<Stay>.Failure("Ban khong co quyen check-out.");
        var stay = await _stays.GetByIdAsync(stayId);
        if (stay == null || stay.Status != StayStatus.Active)
            return ServiceResult<Stay>.Failure("Khong tim thay ky luu tru dang hoat dong.");
        if (stay.ServiceOrders.Any(o => o.Status is ServiceOrderStatus.Pending or ServiceOrderStatus.Processing))
            return ServiceResult<Stay>.Failure("Phai hoan tat hoac huy tat ca don dich vu truoc khi check-out.");
        if (stay.Invoice == null || stay.Invoice.Status != InvoiceStatus.Paid)
            return ServiceResult<Stay>.Failure("Hoa don phai duoc thanh toan day du truoc khi check-out.");
        var result = await _stays.CheckOutAsync(stayId, AppSession.CurrentUser?.Id,
            actualCheckOut ?? DateTime.Now);
        return result == null
            ? ServiceResult<Stay>.Failure("Khong the check-out do trang thai da thay doi.")
            : ServiceResult<Stay>.Success(result, "Check-out thanh cong; phong chuyen sang dang don.");
    }

    private static bool CanOperate() => AppSession.RoleName is "Admin" or "Manager" or "Receptionist";
}
