namespace HotelServiceManagement.Application.DTOs.Reports;

public class OccupancyByFloorResponse
{
    public int Floor { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int ReservedRooms { get; set; }
    public decimal OccupancyRate { get; set; }
}
