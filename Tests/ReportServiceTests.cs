using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;
using Services;

namespace HotelManagement.Tests;

public class ReportServiceTests
{
    [Fact]
    /// <summary>
    /// De bai (docs/PHAN_CONG.md) doi HAI thu: liet ke DU moi ngay trong khoang - ngay khong
    /// phat sinh van co mot dong gia tri 0, khong nhay coc; va sap xep giam dan theo DOANH THU
    /// chu KHONG phai theo ngay. Du lieu gia: 1/7 = 100, 3/7 = 200, 2/7 khong co gi.
    /// </summary>
    public async Task RevenueRows_LietKeDuNgay_VaSapXepGiamDanTheoDoanhThu()
    {
        TestUsers.SignInWithPermissions(PermissionCodes.ReportView);
        var repository = new FakeReportRepository();
        var result = await new ReportService(repository)
            .GetRevenueAsync(new DateTime(2026, 7, 1), new DateTime(2026, 7, 3));

        Assert.True(result.Ok);
        // Du 3 ngay (khong bo ngay 2/7 du hom do khong ban duoc gi)
        Assert.Equal(3, result.Data!.ByDay.Count);
        // Thu tu: 3/7 (200) -> 1/7 (100) -> 2/7 (0)
        Assert.Equal(
            [new DateTime(2026, 7, 3), new DateTime(2026, 7, 1), new DateTime(2026, 7, 2)],
            result.Data.ByDay.Select(x => x.Date).ToArray());
        Assert.Equal(0, result.Data.ByDay.Single(x => x.Date == new DateTime(2026, 7, 2)).InvoiceRevenue);
    }

    [Fact]
    public async Task ExportRevenueCsv_UsesStableInvariantFormat()
    {
        TestUsers.SignInWithPermissions(PermissionCodes.ReportView, PermissionCodes.ReportExport);

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
