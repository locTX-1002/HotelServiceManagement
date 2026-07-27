using BusinessObjects;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

/// <summary>
/// Nhat ky he thong doc tu bang AuditLogs that. Hai truy van duoi day co phep chieu
/// LINQ -> SQL de sai (Distinct tren ca entity, Include long nhau), khong chay that thi
/// khong biet - man hinh chi vo luc nguoi dung mo len.
/// </summary>
public class AuditLogServiceTests
{
    [DbFact]
    public async Task DanhSachNguoiThaoTac_DichDuocSangSql()
    {
        await SignInAdminAsync();

        var result = await new AuditLogService().GetActorsAsync();

        Assert.True(result.Ok, result.Message);
        Assert.NotNull(result.Data);
        // Khong ai lot vao danh sach hai lan
        Assert.Equal(result.Data!.Select(x => x.Id).Distinct().Count(), result.Data.Count);
    }

    [DbFact]
    public async Task TraCuuNhatKy_LayDuocDongTrongKhoangNgay()
    {
        await SignInAdminAsync();

        var result = await new AuditLogService()
            .SearchAsync(DateTime.Today.AddDays(-30), DateTime.Today, null, null);

        Assert.True(result.Ok, result.Message);
        // Da co thao tac quan tri thi phai doc ra duoc; ngay cuoi khoang khong duoc bi cat
        Assert.All(result.Data!, x => Assert.InRange(
            x.CreatedAt, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1)));
    }

    [DbFact]
    public async Task TraCuuNhatKy_NgayBatDauSauNgayKetThuc_BiChan()
    {
        await SignInAdminAsync();

        var result = await new AuditLogService()
            .SearchAsync(DateTime.Today, DateTime.Today.AddDays(-3), null, null);

        Assert.False(result.Ok);
        Assert.Contains("trước ngày kết thúc", result.Message);
    }

    private static async Task SignInAdminAsync()
    {
        await using var db = HotelDbContextFactory.Create();
        var admin = await db.Users.AsNoTracking()
            .Include(x => x.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstAsync(x => x.Role!.RoleName == RoleNames.Admin);
        AppSession.SignIn(admin);
    }
}
