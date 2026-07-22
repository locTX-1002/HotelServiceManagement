namespace Services;

public record RevenueByDay(DateTime Date, decimal RoomRevenue, decimal ServiceRevenue, decimal SurchargeRevenue, decimal DiscountAmount, decimal InvoiceRevenue, decimal CollectedAmount);
public record RevenueReport(DateTime FromDate, DateTime ToDate, decimal RoomRevenue, decimal ServiceRevenue, decimal SurchargeRevenue, decimal DiscountAmount, decimal InvoiceRevenue, decimal CollectedAmount, IReadOnlyList<RevenueByDay> ByDay);
public record OccupancyReport(int TotalRooms, int AvailableRooms, int ReservedRooms, int OccupiedRooms, decimal OccupancyRate);
public interface IReportService { Task<ServiceResult<RevenueReport>> GetRevenueAsync(DateTime from, DateTime to); Task<ServiceResult<string>> ExportRevenueCsvAsync(DateTime from, DateTime to); Task<ServiceResult<OccupancyReport>> GetOccupancyAsync(); }
