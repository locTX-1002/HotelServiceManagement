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
    public IReadOnlyList<ArrivalItem> Arrivals { get; set; } = Array.Empty<ArrivalItem>();
    public IReadOnlyList<DepartureItem> Departures { get; set; } = Array.Empty<DepartureItem>();
    public IReadOnlyList<RevenueDayItem> Revenue7d { get; set; } = Array.Empty<RevenueDayItem>();
    public IReadOnlyList<AlertItem> Alerts { get; set; } = Array.Empty<AlertItem>();
}

public class ArrivalItem
{
    public string BookingCode { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string Eta { get; set; } = string.Empty;
}

public class DepartureItem
{
    public string BookingCode { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public int Nights { get; set; }
    public decimal AmountDue { get; set; }
}

public class RevenueDayItem
{
    public string Day { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class AlertItem
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}
