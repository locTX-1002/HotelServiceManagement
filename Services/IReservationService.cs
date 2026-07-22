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
}
