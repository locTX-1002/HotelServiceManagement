namespace HotelServiceManagement.Application.DTOs.Reports;

public class RevenueByDayResponse
{
    public DateTime Date { get; set; }
    public decimal RoomRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
}
