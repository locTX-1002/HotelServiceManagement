namespace HotelServiceManagement.Application.DTOs.Reports;

public class RevenueReportResponse
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal RoomRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal PaymentRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
}
