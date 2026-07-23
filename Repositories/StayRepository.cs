using BusinessObjects.Entities;
using DataAccessObjects;

namespace Repositories
{
    public class StayRepository : IStayRepository
    {
        public Task<List<Reservation>> GetPendingArrivalsAsync() => StayDAO.Instance.GetPendingArrivalsAsync();
        public Task<List<Stay>> GetActiveAsync() => StayDAO.Instance.GetActiveAsync();

        public Task<(bool Ok, string Message)> CheckInAsync(int reservationId, DateTime actualCheckIn, int checkedInByUserId)
            => StayDAO.Instance.CheckInAsync(reservationId, actualCheckIn, checkedInByUserId);

        public Task<(bool Ok, string Message)> CheckOutAsync(int stayId, int checkedOutByUserId)
            => StayDAO.Instance.CheckOutAsync(stayId, checkedOutByUserId);

        public Task<(bool Ok, string Message)> ExtendAsync(int stayId, DateTime newCheckOut)
            => StayDAO.Instance.ExtendAsync(stayId, newCheckOut);
    }
}
