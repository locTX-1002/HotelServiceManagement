using BusinessObjects;
using Services;

namespace HotelManagement.Tests;

/// <summary>
/// Khoa lai hai yeu cau ghi RO trong de bai (docs/PHAN_CONG.md), de sau nay khong ai
/// sua nham: (1) ngay khong phat sinh van phai co mot dong gia tri 0, khong duoc nhay coc;
/// (2) bang sap xep giam dan theo DOANH THU chu khong phai theo ngay.
/// </summary>
[Collection("Db")]
public class ReportRequirementTests
{
    [DbFact]
    public async Task BaoCao_LietKeDuMoiNgayTrongKhoang_KhongNhayCoc()
    {
        await TestUsers.SignInAsync(RoleNames.Manager);
        try
        {
            // Chon khoang xa trong qua khu de chac chan khong co hoa don nao
            var to = DateTime.Today.AddDays(-400);
            var from = to.AddDays(-6);

            var r = await new ReportService().GetRevenueAsync(from, to);
            Assert.True(r.Ok, r.Message);

            // 7 ngay -> dung 7 dong, ke ca khi khong ban duoc phong nao
            Assert.Equal(7, r.Data!.ByDay.Count);
            Assert.All(r.Data.ByDay, d => Assert.InRange(d.Date, from, to));
            Assert.Equal(7, r.Data.ByDay.Select(d => d.Date).Distinct().Count());
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task BaoCao_NgayBatDauSauNgayKetThuc_BiChan()
    {
        await TestUsers.SignInAsync(RoleNames.Manager);
        try
        {
            var r = await new ReportService().GetRevenueAsync(DateTime.Today, DateTime.Today.AddDays(-5));
            Assert.False(r.Ok);
            Assert.False(string.IsNullOrWhiteSpace(r.Message));
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task BaoCao_ChiAdminVaManagerXemDuoc()
    {
        try
        {
            await TestUsers.SignInAsync(RoleNames.Receptionist);
            var leTan = await new ReportService().GetRevenueAsync(DateTime.Today.AddDays(-7), DateTime.Today);
            Assert.False(leTan.Ok);

            await TestUsers.SignInAsync(RoleNames.Manager);
            var quanLy = await new ReportService().GetRevenueAsync(DateTime.Today.AddDays(-7), DateTime.Today);
            Assert.True(quanLy.Ok, quanLy.Message);
        }
        finally { AppSession.SignOut(); }
    }
}
