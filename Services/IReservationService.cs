using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace Services
{
    public interface IReservationService
    {
        Task<List<Reservation>> GetAllAsync();
        /// <summary>Phòng còn trống trong khoảng ngày (kèm RoomType).</summary>
        Task<ServiceResult<List<Room>>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut);
        Task<ServiceResult<Reservation>> CreateAsync(int guestId, int roomId, int numberOfGuests,
            DateTime checkIn, DateTime checkOut, ReservationStatus status, string? specialRequests, int? createdByUserId);
        Task<ServiceResult> UpdateAsync(int id, int guestId, int roomId, int numberOfGuests,
            DateTime checkIn, DateTime checkOut, ReservationStatus status, string? specialRequests);
        Task<ServiceResult> ConfirmAsync(int id);
        Task<ServiceResult> CancelAsync(int id);
        Task<ServiceResult> NoShowAsync(int id);
    }
}
