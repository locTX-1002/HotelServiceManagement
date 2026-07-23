using BusinessObjects.Entities;

namespace Repositories
{
    public interface IStayRepository
    {
        Task<List<Reservation>> GetPendingArrivalsAsync();
        Task<List<Stay>> GetActiveAsync();
        Task<(bool Ok, string Message)> CheckInAsync(int reservationId, DateTime actualCheckIn, int checkedInByUserId);
        Task<(bool Ok, string Message)> CheckOutAsync(int stayId, int checkedOutByUserId);
        Task<(bool Ok, string Message)> ExtendAsync(int stayId, DateTime newCheckOut);
    }
}
