using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories;

public sealed class StayRepository : IStayRepository
{
    public Task<List<Stay>> GetActiveAsync() => StayDAO.Instance.GetActiveAsync();
    public Task<Stay?> GetByIdAsync(int id) => StayDAO.Instance.GetByIdAsync(id);
    public Task<Stay?> CheckInAsync(int reservationId, int? userId, DateTime actualCheckIn)
        => StayDAO.Instance.CheckInAsync(reservationId, userId, actualCheckIn);
    public Task<Stay?> CheckOutAsync(int stayId, int? userId, DateTime actualCheckOut)
        => StayDAO.Instance.CheckOutAsync(stayId, userId, actualCheckOut);
}
