using BusinessObjects.Entities;
using Repositories;

namespace Services
{
    /// <summary>
    /// Nghiệp vụ Check-in / Check-out - port rule từ web. Guard ngày check-in, lý do cụ thể khi
    /// phòng chưa sẵn sàng; check-out đưa phòng về Đang dọn (hoá đơn tách sang module Hoá đơn).
    /// </summary>
    public class StayService : IStayService
    {
        private readonly IStayRepository _repo = new StayRepository();

        public Task<List<Reservation>> GetArrivalsAsync() => _repo.GetPendingArrivalsAsync();
        public Task<List<Stay>> GetActiveAsync() => _repo.GetActiveAsync();

        public async Task<ServiceResult> CheckInAsync(int reservationId, int checkedInByUserId)
        {
            var (ok, message) = await _repo.CheckInAsync(reservationId, DateTime.Now, checkedInByUserId);
            return ok ? ServiceResult.Success(message) : ServiceResult.Failure(message);
        }

        public async Task<ServiceResult> CheckOutAsync(int stayId, int checkedOutByUserId)
        {
            var (ok, message) = await _repo.CheckOutAsync(stayId, checkedOutByUserId);
            return ok ? ServiceResult.Success(message) : ServiceResult.Failure(message);
        }
    }
}
