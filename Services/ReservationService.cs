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

    public async Task<ServiceResult<Reservation>> CreateAsync(int guestId, int roomId,
        int numberOfGuests, DateTime checkInDate, DateTime checkOutDate, string? specialRequests,
        decimal? depositAmount, PaymentMethod? depositPaymentMethod)
    {
        if (!AuthorizationPolicy.CanOperateFrontDesk)
            return ServiceResult<Reservation>.Failure("Ban khong co quyen tao dat phong.");
        var error = Validate(numberOfGuests, checkInDate, checkOutDate, specialRequests,
            depositAmount, depositPaymentMethod);
        if (error != null) return ServiceResult<Reservation>.Failure(error);

        var guest = await _guests.GetByIdAsync(guestId);
        if (guest == null) return ServiceResult<Reservation>.Failure("Khong tim thay khach hang.");
        var room = await _rooms.GetByIdAsync(roomId);
        var roomError = ValidateRoom(room, numberOfGuests);
        if (roomError != null) return ServiceResult<Reservation>.Failure(roomError);
        if (await _reservations.HasOverlapAsync(roomId, checkInDate, checkOutDate))
            return ServiceResult<Reservation>.Failure("Phong da co lich dat trung khoang thoi gian nay.");

        var entity = new Reservation
        {
            BookingCode = await GenerateCodeAsync(),
            GuestId = guestId,
            RoomId = roomId,
            NumberOfGuests = numberOfGuests,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            Status = ReservationStatus.Pending,
            SpecialRequests = Normalize(specialRequests),
            DepositAmount = depositAmount,
            DepositPaymentMethod = depositAmount > 0 ? depositPaymentMethod : null,
            DepositPaidAt = depositAmount > 0 ? DateTime.Now : null,
            CreatedByUserId = AppSession.CurrentUser?.Id,
        };
        await _reservations.AddAsync(entity);
        entity.Guest = guest;
        entity.Room = room!;
        return ServiceResult<Reservation>.Success(entity, "Da tao dat phong.");
    }

    public async Task<ServiceResult<Reservation>> UpdateAsync(int id, int roomId,
        int numberOfGuests, DateTime checkInDate, DateTime checkOutDate, string? specialRequests)
    {
        if (!AuthorizationPolicy.CanOperateFrontDesk)
            return ServiceResult<Reservation>.Failure("Ban khong co quyen sua dat phong.");
        var error = Validate(numberOfGuests, checkInDate, checkOutDate, specialRequests, null, null);
        if (error != null) return ServiceResult<Reservation>.Failure(error);
        var entity = await _reservations.GetByIdAsync(id);
        if (entity == null) return ServiceResult<Reservation>.Failure("Khong tim thay dat phong.");
        if (entity.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            return ServiceResult<Reservation>.Failure("Trang thai hien tai khong cho phep sua dat phong.");

        var room = await _rooms.GetByIdAsync(roomId);
        var roomError = ValidateRoom(room, numberOfGuests);
        if (roomError != null) return ServiceResult<Reservation>.Failure(roomError);
        if (await _reservations.HasOverlapAsync(roomId, checkInDate, checkOutDate, id))
            return ServiceResult<Reservation>.Failure("Phong da co lich dat trung khoang thoi gian nay.");

        entity.RoomId = roomId;
        entity.NumberOfGuests = numberOfGuests;
        entity.CheckInDate = checkInDate;
        entity.CheckOutDate = checkOutDate;
        entity.SpecialRequests = Normalize(specialRequests);
        Detach(entity);
        await _reservations.UpdateAsync(entity);
        entity.Room = room!;
        return ServiceResult<Reservation>.Success(entity, "Da cap nhat dat phong.");
    }

    public Task<ServiceResult<Reservation>> ConfirmAsync(int id)
        => !AuthorizationPolicy.CanOperateFrontDesk
            ? Task.FromResult(ServiceResult<Reservation>.Failure("Ban khong co quyen xac nhan dat phong."))
            : ChangeStatusAsync(id, ReservationStatus.Pending, ReservationStatus.Confirmed,
            "Chi dat phong dang cho moi co the xac nhan.", "Da xac nhan dat phong.");

    public async Task<ServiceResult<Reservation>> CancelAsync(int id)
    {
        if (!AuthorizationPolicy.CanOperateFrontDesk)
            return ServiceResult<Reservation>.Failure("Ban khong co quyen huy dat phong.");
        var entity = await _reservations.GetByIdAsync(id);
        if (entity == null) return ServiceResult<Reservation>.Failure("Khong tim thay dat phong.");
        if (entity.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            return ServiceResult<Reservation>.Failure("Trang thai hien tai khong cho phep huy dat phong.");
        entity.Status = ReservationStatus.Cancelled;
        Detach(entity);
        await _reservations.UpdateAsync(entity);
        return ServiceResult<Reservation>.Success(entity, "Da huy dat phong.");
    }

    private async Task<ServiceResult<Reservation>> ChangeStatusAsync(int id,
        ReservationStatus expected, ReservationStatus target, string invalid, string success)
    {
        var entity = await _reservations.GetByIdAsync(id);
        if (entity == null) return ServiceResult<Reservation>.Failure("Khong tim thay dat phong.");
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
        if (guests < 1) return "So khach phai lon hon 0.";
        if (checkOut <= checkIn) return "Ngay tra phong phai sau ngay nhan phong.";
        if (!string.IsNullOrWhiteSpace(requests) && requests.Trim().Length > 500)
            return "Yeu cau dac biet toi da 500 ky tu.";
        if (deposit < 0) return "Tien coc khong duoc am.";
        if (deposit > 0 && method == null) return "Phai chon phuong thuc thanh toan tien coc.";
        if (method != null && !Enum.IsDefined(method.Value)) return "Phuong thuc thanh toan khong hop le.";
        return null;
    }

    private static string? ValidateRoom(Room? room, int guests)
    {
        if (room == null) return "Khong tim thay phong.";
        if (!room.IsActive) return "Phong da ngung hoat dong.";
        if (room.Status == RoomStatus.Maintenance) return "Phong dang bao tri.";
        if (!room.RoomType.IsActive) return "Loai phong da ngung hoat dong.";
        return guests > room.RoomType.Capacity ? "So khach vuot suc chua cua loai phong." : null;
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
        if (checkOut <= checkIn) return ServiceResult<List<Room>>.Failure("Ngay tra phai sau ngay nhan.");
        var rooms = await _reservations.GetAvailableRoomsAsync(checkIn, checkOut);
        return ServiceResult<List<Room>>.Success(rooms);
    }

    public async Task<ServiceResult<Reservation>> NoShowAsync(int id)
    {
        var reservation = await _reservations.GetByIdAsync(id);
        if (reservation == null) return ServiceResult<Reservation>.Failure("Khong tim thay dat phong.");
        if (reservation.Status != ReservationStatus.Confirmed)
            return ServiceResult<Reservation>.Failure("Chi dat phong da xac nhan moi danh dau Khong den duoc.");
        if (reservation.Stay != null)
            return ServiceResult<Reservation>.Failure("Dat phong da co luot luu tru, khong danh dau Khong den duoc.");

        reservation.Status = ReservationStatus.NoShow;
        await _reservations.UpdateAsync(reservation);
        return ServiceResult<Reservation>.Success(reservation, "Da danh dau khach khong den.");
    }

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
