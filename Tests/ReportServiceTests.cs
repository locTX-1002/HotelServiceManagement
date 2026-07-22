using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;
using Services;

namespace HotelManagement.Tests;

public class ReportServiceTests
{
    [Fact]
    public async Task RevenueRows_AreSortedByDateDescending()
    {
        AppSession.SignIn(new User { Role = new Role { RoleName = "Manager" } });
        var repository = new FakeReportRepository();
        var result = await new ReportService(repository)
            .GetRevenueAsync(new DateTime(2026, 7, 1), new DateTime(2026, 7, 3));

        Assert.True(result.Ok);
        Assert.Equal([new DateTime(2026, 7, 3), new DateTime(2026, 7, 1)],
            result.Data!.ByDay.Select(x => x.Date).ToArray());
    }

    [Fact]
    public async Task ExportRevenueCsv_UsesStableInvariantFormat()
    {
        AppSession.SignIn(new User { Role = new Role { RoleName = "Manager" } });

        var result = await new ReportService(new FakeReportRepository())
            .ExportRevenueCsvAsync(new DateTime(2026, 7, 1), new DateTime(2026, 7, 3));

        Assert.True(result.Ok);
        Assert.StartsWith("Date,RoomRevenue", result.Data);
        Assert.Contains("2026-07-03", result.Data);
    }

    private sealed class FakeReportRepository : IReportRepository
    {
        public Task<List<Invoice>> GetInvoicesAsync(DateTime from, DateTime to) => Task.FromResult(new List<Invoice>
        {
            new() { InvoiceDate = new DateTime(2026,7,1), TotalAmount = 100 },
            new() { InvoiceDate = new DateTime(2026,7,3), TotalAmount = 200 }
        });
        public Task<List<Payment>> GetPaymentsAsync(DateTime from, DateTime to) => Task.FromResult(new List<Payment>());
        public Task<List<Room>> GetRoomsAsync() => Task.FromResult(new List<Room>());
    }
}
