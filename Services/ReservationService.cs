using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;

namespace Services;

public sealed class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservations;
    private readonly IGuestRepository _guests;
    private readonly IRoomRepository _rooms;

    public ReservationService() : this(new ReservationRepository(), new GuestRepository(), new RoomRepository()) { }
    public ReservationService(IReservationRepository reservations, IGuestRepository guests, IRoomRepository rooms)
    {
        _reservations = reservations;
        _guests = guests;
        _rooms = rooms;
    }

    public Task<List<Reservation>> GetAllAsync() => _reservations.GetAllAsync();

    public async Task<ServiceResult<List<Reservation>>> GetMyReservationsAsync()
    {
        var guestId = AppSession.CurrentGuestId;
        if (guestId == null)
        {
            return ServiceResult<List<Reservation>>.Failure("Chưa đăng nhập bằng tài khoản khách hàng.");
        }
        return ServiceResult<List<Reservation>>.Success(await _reservations.GetByGuestAsync(guestId.Value));
    }

    public async Task<ServiceResult<Reservation>> CreateAsync(int guestId, int roomId,
        int numberOfGuests, DateTime checkInDate, DateTime checkOutDate, string? specialRequests,
        decimal? depositAmount, PaymentMethod? depositPaymentMethod,
        ReservationStatus initialStatus = ReservationStatus.Pending)
    {
        if (!AuthorizationPolicy.HasPermission(PermissionCodes.ReservationCreate))
            return ServiceResult<Reservation>.Failure("Bạn không có quyền tạo đặt phòng.");
        if (initialStatus is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            return ServiceResult<Reservation>.Failure("Trạng thái khởi tạo không hợp lệ.");
        var error = Validate(numberOfGuests, checkInDate, checkOutDate, specialRequests,
            depositAmount, depositPaymentMethod);
        if (error != null) return ServiceResult<Reservation>.Failure(error);

        var guest = await _guests.GetByIdAsync(guestId);
        if (guest == null) return ServiceResult<Reservation>.Failure("Không tìm thấy khách hàng.");
        var room = await _rooms.GetByIdAsync(roomId);
        var roomError = ValidateRoom(room, numberOfGuests);
        if (roomError != null) return ServiceResult<Reservation>.Failure(roomError);
        if (await _reservations.HasOverlapAsync(roomId, checkInDate, checkOutDate))
            return ServiceResult<Reservation>.Failure("Phòng đã có lịch đặt trùng khoảng thời gian này.");

        // Coc khong duoc vuot tien phong ca ky. Neu vuot, luc lap hoa don service se tu choi
        // ("Tien coc vuot tong hoa don") ma khong co hoa don thi cung khong tra phong duoc -
        // khach ket lai trong phong. Chan ngay tu luc nhap thi khong bao gio roi vao the do.
        if (depositAmount is > 0)
        {
            var nights = Math.Max(1, (checkOutDate.Date - checkInDate.Date).Days);
            var roomCharge = nights * room!.RoomType.BasePrice;
            if (depositAmount > roomCharge)
                return ServiceResult<Reservation>.Failure(
                    $"Tiền cọc ({depositAmount:N0} đ) không được vượt tiền phòng cả kỳ ({roomCharge:N0} đ).");
        }

        var entity = new Reservation
        {
            BookingCode = await GenerateCodeAsync(),
            GuestId = guestId,
            RoomId = roomId,
            NumberOfGuests = numberOfGuests,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            Status = initialStatus,
            SpecialRequests = Normalize(specialRequests),
            DepositAmount = depositAmount,
            DepositPaymentMethod = depositAmount > 0 ? depositPaymentMethod : null,
            DepositPaidAt = depositAmount > 0 ? DateTime.Now : null,
            CreatedByUserId = AppSession.CurrentUser?.Id,
        };
        await _reservations.AddAsync(entity);
        entity.Guest = guest;
        entity.Room = room!;
        return ServiceResult<Reservation>.Success(entity, "Đã tạo đặt phòng.");
    }

    public async Task<ServiceResult<Reservation>> UpdateAsync(int id, int roomId,
        int numberOfGuests, DateTime checkInDate, DateTime checkOutDate, string? specialRequests)
    {
        if (!AuthorizationPolicy.HasPermission(PermissionCodes.ReservationUpdate))
            return ServiceResult<Reservation>.Failure("Bạn không có quyền sửa đặt phòng.");
        var error = Validate(numberOfGuests, checkInDate, checkOutDate, specialRequests, null, null);
        if (error != null) return ServiceResult<Reservation>.Failure(error);
        var entity = await _reservations.GetByIdAsync(id);
        if (entity == null) return ServiceResult<Reservation>.Failure("Không tìm thấy đặt phòng.");
        if (entity.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            return ServiceResult<Reservation>.Failure("Trạng thái hiện tại không cho phép sửa đặt phòng.");

        var room = await _rooms.GetByIdAsync(roomId);
        var roomError = ValidateRoom(room, numberOfGuests);
        if (roomError != null) return ServiceResult<Reservation>.Failure(roomError);
        if (await _reservations.HasOverlapAsync(roomId, checkInDate, checkOutDate, id))
            return ServiceResult<Reservation>.Failure("Phòng đã có lịch đặt trùng khoảng thời gian này.");

        entity.RoomId = roomId;
        entity.NumberOfGuests = numberOfGuests;
        entity.CheckInDate = checkInDate;
        entity.CheckOutDate = checkOutDate;
        entity.SpecialRequests = Normalize(specialRequests);
        Detach(entity);
        await _reservations.UpdateAsync(entity);
        entity.Room = room!;
        return ServiceResult<Reservation>.Success(entity, "Đã cập nhật đặt phòng.");
    }

    public Task<ServiceResult<Reservation>> ConfirmAsync(int id)
        => !AuthorizationPolicy.HasPermission(PermissionCodes.ReservationUpdate)
            ? Task.FromResult(ServiceResult<Reservation>.Failure("Bạn không có quyền xác nhận đặt phòng."))
            : ChangeStatusAsync(id, ReservationStatus.Pending, ReservationStatus.Confirmed,
            "Chỉ đặt phòng đang chờ mới xác nhận được.", "Đã xác nhận đặt phòng.");

    public async Task<ServiceResult<Reservation>> CancelAsync(int id)
    {
        if (!AuthorizationPolicy.HasPermission(PermissionCodes.ReservationCancelApprove))
            return ServiceResult<Reservation>.Failure("Bạn không có quyền huỷ đặt phòng.");
        var entity = await _reservations.GetByIdAsync(id);
        if (entity == null) return ServiceResult<Reservation>.Failure("Không tìm thấy đặt phòng.");
        if (entity.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            return ServiceResult<Reservation>.Failure("Trạng thái hiện tại không cho phép huỷ đặt phòng.");
        entity.Status = ReservationStatus.Cancelled;
        Detach(entity);
        await _reservations.UpdateAsync(entity);
        return ServiceResult<Reservation>.Success(entity, "Đã huỷ đặt phòng.");
    }

    private async Task<ServiceResult<Reservation>> ChangeStatusAsync(int id,
        ReservationStatus expected, ReservationStatus target, string invalid, string success)
    {
        var entity = await _reservations.GetByIdAsync(id);
        if (entity == null) return ServiceResult<Reservation>.Failure("Không tìm thấy đặt phòng.");
        if (entity.Status != expected) return ServiceResult<Reservation>.Failure(invalid);
        entity.Status = target;
        Detach(entity);
        await _reservations.UpdateAsync(entity);
        return ServiceResult<Reservation>.Success(entity, success);
    }

    private async Task<string> GenerateCodeAsync()
    {
        for (var i = 0; i < 10; i++)
        {
            var code = $"BK{DateTime.Now:yyyyMMdd}{Random.Shared.Next(1000, 10000)}";
            if (!await _reservations.BookingCodeExistsAsync(code)) return code;
        }
        return $"BK{Guid.NewGuid():N}"[..20].ToUpperInvariant();
    }

    private static string? Validate(int guests, DateTime checkIn, DateTime checkOut,
        string? requests, decimal? deposit, PaymentMethod? method)
    {
        if (guests < 1) return "Số khách phải lớn hơn 0.";
        if (checkOut <= checkIn) return "Ngày trả phòng phải sau ngày nhận phòng.";
        if (!string.IsNullOrWhiteSpace(requests) && requests.Trim().Length > 500)
            return "Yêu cầu đặc biệt tối đa 500 ký tự.";
        if (deposit < 0) return "Tiền cọc không được âm.";
        if (deposit > 0 && method == null) return "Phải chọn phương thức thanh toán tiền cọc.";
        if (method != null && !Enum.IsDefined(method.Value)) return "Phương thức thanh toán không hợp lệ.";
        return null;
    }

    private static string? ValidateRoom(Room? room, int guests)
    {
        if (room == null) return "Không tìm thấy phòng.";
        if (!room.IsActive) return "Phòng đã ngừng hoạt động.";
        if (room.Status == RoomStatus.Maintenance) return "Phòng đang bảo trì.";
        if (!room.RoomType.IsActive) return "Loại phòng đã ngừng hoạt động.";
        return guests > room.RoomType.Capacity ? "Số khách vượt sức chứa của loại phòng." : null;
    }

    private static void Detach(Reservation entity)
    {
        entity.Guest = null!;
        entity.Room = null!;
        entity.CreatedByUser = null;
        entity.Stay = null;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<ServiceResult<List<Room>>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut)
    {
        if (checkOut <= checkIn) return ServiceResult<List<Room>>.Failure("Ngày trả phải sau ngày nhận.");
        var rooms = await _reservations.GetAvailableRoomsAsync(checkIn, checkOut);
        return ServiceResult<List<Room>>.Success(rooms);
    }

    public async Task<ServiceResult<Reservation>> NoShowAsync(int id)
    {
        if (!AuthorizationPolicy.CanOperateFrontDesk)
            return ServiceResult<Reservation>.Failure("Bạn không có quyền chuyển trạng thái Không đến.");
        var reservation = await _reservations.GetByIdAsync(id);
        if (reservation == null) return ServiceResult<Reservation>.Failure("Không tìm thấy đặt phòng.");
        // Nhan ca don CHO xac nhan: don rac thi le tan phai xac nhan mot cai vo nghia roi
        // moi danh dau duoc - hai thao tac cho mot don khong ai den.
        if (reservation.Status is not (ReservationStatus.Confirmed or ReservationStatus.Pending))
            return ServiceResult<Reservation>.Failure(
                "Chỉ đặt phòng đang chờ hoặc đã xác nhận mới đánh dấu Không đến được.");
        if (reservation.Stay != null)
            return ServiceResult<Reservation>.Failure("Đặt phòng đã có lượt lưu trú, không đánh dấu Không đến được.");

        reservation.Status = ReservationStatus.NoShow;
        await _reservations.UpdateAsync(reservation);
        return ServiceResult<Reservation>.Success(reservation, "Đã đánh dấu khách không đến.");
    }

    /// <summary>
    /// Don da qua ngay nhan phong ma khach khong den thi chuyen sang Khong den. Goi mot lan
    /// luc app khoi dong. Khong dung quyen vi day la viec don dep cua he thong, khong phai
    /// hanh dong cua nguoi dung.
    /// </summary>
    public Task<int> SweepNoShowAsync() => _reservations.SweepNoShowAsync(DateTime.Today);

    /// <summary>Ten trang thai tieng Viet dung chung cho danh sach va lich phong.</summary>
    public static string StatusText(ReservationStatus status) => status switch
    {
        ReservationStatus.Pending => "Chờ xác nhận",
        ReservationStatus.Confirmed => "Đã xác nhận",
        ReservationStatus.CheckedIn => "Đã check-in",
        ReservationStatus.Completed => "Hoàn tất",
        ReservationStatus.Cancelled => "Đã huỷ",
        ReservationStatus.NoShow => "Không đến",
        _ => status.ToString(),
    };
}
