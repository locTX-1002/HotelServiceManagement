namespace HotelServiceManagement.Application.DTOs.Reports;

public class OccupancyReportResponse
{
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int ReservedRooms { get; set; }
    public decimal OccupancyRate { get; set; }
    public IReadOnlyList<OccupancyByFloorResponse> ByFloor { get; set; } = Array.Empty<OccupancyByFloorResponse>();
}
