using BusinessObjects.Entities;

namespace Services;

public interface IStayService
{
    Task<List<Stay>> GetActiveAsync();
    Task<ServiceResult<Stay>> CheckInAsync(int reservationId, DateTime? actualCheckIn = null);
    Task<ServiceResult<Stay>> CheckOutAsync(int stayId, DateTime? actualCheckOut = null);

    /// <summary>Don da xac nhan ma khach chua den quay - danh sach cho check-in.</summary>
    Task<List<Reservation>> GetArrivalsAsync();

    /// <summary>Gia han (hoac rut ngan) ngay tra cho khach dang o.</summary>
    Task<ServiceResult> ExtendAsync(int stayId, DateTime newCheckOut);
}
