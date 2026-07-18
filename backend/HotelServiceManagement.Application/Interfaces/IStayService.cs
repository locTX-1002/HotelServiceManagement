using System.Threading.Tasks;
using HotelServiceManagement.Application.DTOs.Stays;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IStayService
    {
        Task<IReadOnlyList<ActiveStayResponse>> GetActiveAsync();
        Task<CheckOutResponse> CheckInAsync(CheckInRequest request, int checkedInByUserId);
        Task<CheckOutResponse> CheckOutAsync(int stayId, CheckOutRequest request, int checkedOutByUserId);
    }
}
