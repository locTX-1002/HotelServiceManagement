using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Reports;

namespace HotelServiceManagement.Application.Interfaces
{
    public interface IReportService
    {
        Task<AuthServiceResult<DashboardReportResponse>> GetDashboardAsync();
        Task<AuthServiceResult<OccupancyReportResponse>> GetOccupancyAsync();
        Task<AuthServiceResult<RevenueReportResponse>> GetRevenueAsync(DateTime? fromDate, DateTime? toDate);
    }
}
