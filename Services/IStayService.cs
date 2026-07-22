using BusinessObjects.Entities;

namespace Services;

public interface IStayService
{
    Task<List<Stay>> GetActiveAsync();
    Task<ServiceResult<Stay>> CheckInAsync(int reservationId, DateTime? actualCheckIn = null);
    Task<ServiceResult<Stay>> CheckOutAsync(int stayId, DateTime? actualCheckOut = null);
}
