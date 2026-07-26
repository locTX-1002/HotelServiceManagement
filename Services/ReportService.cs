using BusinessObjects;
using BusinessObjects.Enums;
using Repositories;
using System.Globalization;
using System.Text;
namespace Services;

public sealed class ReportService : IReportService
{
    private readonly IReportRepository _r; public ReportService() : this(new ReportRepository()) { }
    public ReportService(IReportRepository r) => _r = r;
    /// <summary>
    /// Doanh thu theo khoang ngay. ByDay liet ke DU moi ngay trong khoang, ke ca ngay khong
    /// phat sinh (hien 0) - truoc day chi lay ngay CO hoa don nen bang nhay coc, chon 7 ngay
    /// ma chi ra 2-3 dong, nguoi xem tuong mat du lieu.
    /// Sap xep GIAM DAN THEO DOANH THU (chu trong de bai, xem docs/PHAN_CONG.md), khong phai theo ngay.
    /// </summary>
    public async Task<ServiceResult<RevenueReport>> GetRevenueAsync(DateTime from, DateTime to) { if (AppSession.RoleName is not (RoleNames.Admin or RoleNames.Manager)) return ServiceResult<RevenueReport>.Failure("Bạn không có quyền xem báo cáo."); from = from.Date; to = to.Date; if (to < from) return ServiceResult<RevenueReport>.Failure("Ngày kết thúc phải bằng hoặc sau ngày bắt đầu."); var end = to.AddDays(1); var invoices = await _r.GetInvoicesAsync(from, end); var payments = await _r.GetPaymentsAsync(from, end); var days = Enumerable.Range(0, (to - from).Days + 1).Select(i => from.AddDays(i)).Select(d => { var di = invoices.Where(x => x.InvoiceDate.Date == d).ToList(); return new RevenueByDay(d, di.Sum(x => x.RoomCharge), di.Sum(x => x.ServiceCharge), di.Sum(x => x.SurchargeAmount), di.Sum(x => x.DiscountAmount), di.Sum(x => x.TotalAmount), payments.Where(x => x.PaymentDate.Date == d).Sum(x => x.Amount)); }).OrderByDescending(x => x.InvoiceRevenue).ThenByDescending(x => x.Date).ToList(); var report = new RevenueReport(from, to, invoices.Sum(x => x.RoomCharge), invoices.Sum(x => x.ServiceCharge), invoices.Sum(x => x.SurchargeAmount), invoices.Sum(x => x.DiscountAmount), invoices.Sum(x => x.TotalAmount), payments.Sum(x => x.Amount), days); return ServiceResult<RevenueReport>.Success(report); }
    public async Task<ServiceResult<string>> ExportRevenueCsvAsync(DateTime from, DateTime to) { var result = await GetRevenueAsync(from, to); if (!result.Ok || result.Data == null) return ServiceResult<string>.Failure(result.Message); var b = new StringBuilder("Date,RoomRevenue,ServiceRevenue,SurchargeRevenue,DiscountAmount,InvoiceRevenue,CollectedAmount\r\n"); foreach (var x in result.Data.ByDay) b.AppendLine(string.Join(',', x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), x.RoomRevenue.ToString(CultureInfo.InvariantCulture), x.ServiceRevenue.ToString(CultureInfo.InvariantCulture), x.SurchargeRevenue.ToString(CultureInfo.InvariantCulture), x.DiscountAmount.ToString(CultureInfo.InvariantCulture), x.InvoiceRevenue.ToString(CultureInfo.InvariantCulture), x.CollectedAmount.ToString(CultureInfo.InvariantCulture))); return ServiceResult<string>.Success(b.ToString(), "Đã tạo nội dung CSV."); }
    public async Task<ServiceResult<OccupancyReport>> GetOccupancyAsync() { if (AppSession.RoleName is not (RoleNames.Admin or RoleNames.Manager)) return ServiceResult<OccupancyReport>.Failure("Bạn không có quyền xem báo cáo."); var rooms = await _r.GetRoomsAsync(); var occupied = rooms.Count(x => x.Status == RoomStatus.Occupied); var reserved = rooms.Count(x => x.Status == RoomStatus.Reserved); var rate = rooms.Count == 0 ? 0 : Math.Round((decimal)(occupied + reserved) / rooms.Count * 100, 2); return ServiceResult<OccupancyReport>.Success(new OccupancyReport(rooms.Count, rooms.Count(x => x.Status == RoomStatus.Available), reserved, occupied, rate)); }
}
