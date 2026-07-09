namespace HotelServiceManagement.Application.DTOs.Reports;

public class DashboardReportResponse
{
    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int ReservedRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int TodayBookings { get; set; }
    public int ActiveStays { get; set; }
    public decimal TotalRevenue { get; set; }
}
