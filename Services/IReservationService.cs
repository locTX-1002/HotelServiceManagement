using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Services;

public interface IReservationService
{
    Task<List<Reservation>> GetAllAsync();
    Task<ServiceResult<Reservation>> CreateAsync(int guestId, int roomId, int numberOfGuests,
        DateTime checkInDate, DateTime checkOutDate, string? specialRequests,
        decimal? depositAmount, PaymentMethod? depositPaymentMethod);
    Task<ServiceResult<Reservation>> UpdateAsync(int id, int roomId, int numberOfGuests,
        DateTime checkInDate, DateTime checkOutDate, string? specialRequests);
    Task<ServiceResult<Reservation>> ConfirmAsync(int id);
    Task<ServiceResult<Reservation>> CancelAsync(int id);

    /// <summary>Phong con trong trong khoang ngay - man Dat phong, Lich phong, Trang chu deu goi.</summary>
    Task<ServiceResult<List<Room>>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut);

    /// <summary>Danh dau khach khong den - chi tu don da xac nhan.</summary>
    Task<ServiceResult<Reservation>> NoShowAsync(int id);
}
