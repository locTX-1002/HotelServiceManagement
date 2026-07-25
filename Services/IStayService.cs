using BusinessObjects.Entities;

namespace Services;

public interface IStayService
{
    Task<List<Stay>> GetActiveAsync();

    /// <summary>
    /// Danh sach cho man Hoa don: dang o, HOAC da tra phong ma con no tien.
    /// Thieu ve thu hai thi luot da tra phong con no se bien mat, khong thu duoc nua.
    /// </summary>
    Task<List<Stay>> GetBillableAsync();
    Task<ServiceResult<Stay>> CheckInAsync(int reservationId, DateTime? actualCheckIn = null);
    Task<ServiceResult<Stay>> CheckOutAsync(int stayId, DateTime? actualCheckOut = null);

    /// <summary>Don da xac nhan ma khach chua den quay - danh sach cho check-in.</summary>
    Task<List<Reservation>> GetArrivalsAsync();

    /// <summary>Gia han (hoac rut ngan) ngay tra cho khach dang o.</summary>
    Task<ServiceResult> ExtendAsync(int stayId, DateTime newCheckOut);
}
