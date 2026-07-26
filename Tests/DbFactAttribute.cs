using DataAccessObjects;

namespace HotelManagement.Tests;

/// <summary>
/// Danh dau test TICH HOP - can SQL Server that + database FUHotelManagementDB.
///
/// Tren may lap trinh vien (co SQL Server) thi chay binh thuong.
/// Tren CI (GitHub Actions windows-latest KHONG co SQL Server) thi tu bo qua thay vi
/// bao do — de build van xanh ma khong phai xoa test hay dung mock gia.
/// Kiem tra ket noi DUNG MOT LAN cho ca phien test (Lazy) de khong lam cham.
/// </summary>
public sealed class DbFactAttribute : FactAttribute
{
    private static readonly Lazy<string?> LyDoBoQua = new(() =>
    {
        try
        {
            using var db = HotelDbContextFactory.Create();
            return db.Database.CanConnect()
                ? null
                : "Bo qua test tich hop: khong ket noi duoc SQL Server (may nay chua co database).";
        }
        catch (Exception ex)
        {
            return $"Bo qua test tich hop: khong ket noi duoc SQL Server ({ex.GetType().Name}).";
        }
    });

    public DbFactAttribute()
    {
        if (LyDoBoQua.Value != null)
        {
            Skip = LyDoBoQua.Value;
        }
    }
}
