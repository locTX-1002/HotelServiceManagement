using System.Threading.Tasks;
using HotelServiceManagement.Application.DTOs.Stays;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IStayService
    {
        Task<CheckOutResponse> CheckInAsync(CheckInRequest request);
        Task<CheckOutResponse> CheckOutAsync(int stayId);
    }
}
