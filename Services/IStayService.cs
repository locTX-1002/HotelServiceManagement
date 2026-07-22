using BusinessObjects.Entities;

namespace Services
{
    public interface IStayService
    {
        /// <summary>Đặt phòng đã xác nhận, chờ check-in ("hôm nay đến").</summary>
        Task<List<Reservation>> GetArrivalsAsync();
        /// <summary>Lượt đang lưu trú ("đang ở").</summary>
        Task<List<Stay>> GetActiveAsync();
        Task<ServiceResult> CheckInAsync(int reservationId, int checkedInByUserId);
        Task<ServiceResult> CheckOutAsync(int stayId, int checkedOutByUserId);
    }
}
