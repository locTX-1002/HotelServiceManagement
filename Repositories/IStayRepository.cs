using BusinessObjects.Entities;

namespace Repositories;

public interface IStayRepository
{
    Task<List<Stay>> GetActiveAsync();
    Task<Stay?> GetByIdAsync(int id);
    Task<Stay?> CheckInAsync(int reservationId, int? userId, DateTime actualCheckIn);
    Task<Stay?> CheckOutAsync(int stayId, int? userId, DateTime actualCheckOut);
    Task<List<Reservation>> GetArrivalsAsync();
    Task<(bool Ok, string Message)> ExtendAsync(int stayId, DateTime newCheckOut);
}
