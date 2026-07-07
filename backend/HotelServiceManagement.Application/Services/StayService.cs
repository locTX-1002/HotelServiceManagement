using System;
using System.Threading.Tasks;
using HotelServiceManagement.Application.DTOs.Stays;
using HotelServiceManagement.Application.Interfaces;

namespace HotelServiceManagement.Application.Services
{
    public class StayService : IStayService
    {
        public async Task<CheckOutResponse> CheckInAsync(CheckInRequest request)
        {
            // Placeholder/Skeleton response
            return new CheckOutResponse
            {
                StayId = 1,
                ActualCheckOut = DateTime.MinValue,
                IsSuccess = true,
                Message = "Check-in placeholder successful."
            };
        }

        public async Task<CheckOutResponse> CheckOutAsync(int stayId)
        {
            // Placeholder/Skeleton response
            return new CheckOutResponse
            {
                StayId = stayId,
                ActualCheckOut = DateTime.UtcNow,
                TotalRoomCharges = 200.00m,
                TotalServiceCharges = 50.00m,
                TotalAmount = 250.00m,
                IsSuccess = true,
                Message = "Check-out placeholder successful."
            };
        }
    }
}
