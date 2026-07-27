using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

/// <summary>Tu dang ky GuestAccount tu man Login, khong can nhan vien tao ho so truoc.</summary>
[Collection("Db")]
public sealed class GuestRegistrationTests : IAsyncLifetime
{
    private const string Password = "KhachMoi@2026";
    private readonly List<int> _guestIds = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        AppSession.SignOut();
        if (_guestIds.Count == 0) return;

        await using var db = HotelDbContextFactory.Create();
        await db.GuestAccounts.Where(x => _guestIds.Contains(x.GuestId)).ExecuteDeleteAsync();
        await db.Guests.Where(x => _guestIds.Contains(x.Id)).ExecuteDeleteAsync();
    }

    [DbFact]
    public async Task KhachMoi_TuDangKy_TaoGuestVaAccount_VaDangNhapDuoc()
    {
        var phone = NewPhone();
        var service = new GuestAccountService();

        var result = await service.RegisterAsync(
            "Nguyễn Khách Mới",
            phone,
            "khachmoi@test.local",
            Password,
            Password);

        Assert.True(result.Ok, result.Message);
        Assert.NotNull(result.Data);
        _guestIds.Add(result.Data!.GuestId);
        Assert.Equal(phone, result.Data.Guest.PhoneNumber);
        Assert.Equal("Nguyễn Khách Mới", result.Data.Guest.FullName);
        Assert.Equal(GuestTag.None, result.Data.Guest.Tag);

        await using (var db = HotelDbContextFactory.Create())
        {
            var guest = await db.Guests.AsNoTracking().SingleAsync(x => x.Id == result.Data.GuestId);
            var account = await db.GuestAccounts.AsNoTracking().SingleAsync(x => x.GuestId == guest.Id);
            Assert.Equal(phone, guest.PhoneNumber);
            Assert.NotNull(account.PasswordHash);
        }

        var login = await service.LoginAsync(phone, Password);
        Assert.True(login.Ok, login.Message);
        Assert.Equal(result.Data.GuestId, login.Data!.GuestId);
    }

    [DbFact]
    public async Task DangKyTrungSoDienThoai_BiChan_KhongTaoGuestThuHai()
    {
        var phone = NewPhone();
        var service = new GuestAccountService();

        var first = await service.RegisterAsync("Khách A", phone, null, Password, Password);
        Assert.True(first.Ok, first.Message);
        _guestIds.Add(first.Data!.GuestId);

        var second = await service.RegisterAsync("Khách B", phone, null, Password, Password);
        Assert.False(second.Ok);
        Assert.Contains("đã có tài khoản", second.Message);

        await using var db = HotelDbContextFactory.Create();
        Assert.Equal(1, await db.Guests.CountAsync(x => x.PhoneNumber == phone));
        Assert.Equal(1, await db.GuestAccounts.CountAsync(x => x.Guest.PhoneNumber == phone));
    }

    [DbFact]
    public async Task SoDienThoaiDaCoHoSoNhungChuaCoAccount_KhongDuocTuChiemHoSo()
    {
        var phone = NewPhone();
        await using (var db = HotelDbContextFactory.Create())
        {
            var guest = new Guest
            {
                FullName = "Khách đã có hồ sơ",
                PhoneNumber = phone,
                Tag = GuestTag.None
            };
            db.Guests.Add(guest);
            await db.SaveChangesAsync();
            _guestIds.Add(guest.Id);
        }

        var result = await new GuestAccountService().RegisterAsync(
            "Người khác", phone, null, Password, Password);

        Assert.False(result.Ok);
        Assert.Contains("liên hệ lễ tân", result.Message);

        await using var verify = HotelDbContextFactory.Create();
        Assert.Equal(1, await verify.Guests.CountAsync(x => x.PhoneNumber == phone));
        Assert.Equal(0, await verify.GuestAccounts.CountAsync(x => x.Guest.PhoneNumber == phone));
    }

    [DbFact]
    public async Task DangKy_MatKhauXacNhanSai_BiChanTruocKhiGhiDb()
    {
        var phone = NewPhone();

        var result = await new GuestAccountService().RegisterAsync(
            "Khách Test", phone, null, Password, "Khac@2026");

        Assert.False(result.Ok);
        Assert.Contains("xác nhận", result.Message);

        await using var db = HotelDbContextFactory.Create();
        Assert.False(await db.Guests.AnyAsync(x => x.PhoneNumber == phone));
    }

    private static string NewPhone()
        => $"0{Random.Shared.NextInt64(900_000_000, 999_999_999)}";
}
