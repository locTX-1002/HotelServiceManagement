using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

/// <summary>
/// Test tich hop chay THAT tren SQL Server (FUHotelManagementDB): khach hang -> tai khoan khach
/// -> dat phong. Khong mock, khong in-memory: moi service goi thang xuong DAO that.
/// Moi test tu tao du lieu rieng (ten/SDT/CCCD ngau nhien) va xoa sach o DisposeAsync,
/// khong dung vao 8 khach / 11 don seed san dang dung de demo.
/// </summary>
[Collection("Db")]
public class GuestReservationFlowTests : IAsyncLifetime
{
    private const string EmailLeTan = "receptionist@hotel.com";
    private const string EmailNhanVienDichVu = "service@hotel.com";

    // Moc thoi gian rat xa de khong bao gio dam vao lich cua du lieu seed.
    private static readonly DateTime MocXa = DateTime.Today.AddYears(3);

    private readonly IGuestService _guests = new GuestService();
    private readonly IGuestAccountService _accounts = new GuestAccountService();
    private readonly IReservationService _reservations = new ReservationService();

    // Id cua moi khach do test nay tao ra - dung de don dep (booking + tai khoan + ho so).
    private readonly List<int> _guestIds = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        AppSession.SignOut();
        if (_guestIds.Count == 0) return;

        await using var db = HotelDbContextFactory.Create();

        // Xoa booking truoc vi FK Guest dat OnDelete(Restrict) - con booking thi khong xoa duoc khach.
        var booking = await db.Reservations.Where(r => _guestIds.Contains(r.GuestId)).ToListAsync();
        db.Reservations.RemoveRange(booking);
        var taiKhoan = await db.GuestAccounts.Where(a => _guestIds.Contains(a.GuestId)).ToListAsync();
        db.GuestAccounts.RemoveRange(taiKhoan);
        await db.SaveChangesAsync();

        var khach = await db.Guests.Where(g => _guestIds.Contains(g.Id)).ToListAsync();
        db.Guests.RemoveRange(khach);
        await db.SaveChangesAsync();
    }

    // ----------------------------------------------------------------- KHACH HANG

    [DbFact]
    public async Task TaoKhach_RoiTimKiemTheoTenVaSdt_RaDungKhachVuaTao()
    {
        await DangNhapAsync(EmailLeTan);
        var ten = TenNgauNhien();
        var sdt = SdtNgauNhien();

        var tao = await _guests.CreateAsync(ten, EmailNgauNhien(), sdt, CccdNgauNhien(), GuestTag.None, null);

        Assert.True(tao.Ok, tao.Message);
        GhiNhoDeDonDep(tao.Data!.Id);

        var theoSdt = await _guests.SearchAsync(sdt);
        Assert.Single(theoSdt);
        Assert.Equal(tao.Data.Id, theoSdt[0].Id);

        var theoTen = await _guests.SearchAsync(ten);
        Assert.Contains(theoTen, g => g.Id == tao.Data.Id);

        var chinhXac = await _guests.FindExactAsync(sdt);
        Assert.NotNull(chinhXac);
        Assert.Equal(ten, chinhXac!.FullName);
    }

    [DbFact]
    public async Task SuaHoSoKhach_DoiTenEmailCccd_LuuXuongDbVaDocLaiDung()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();

        var tenMoi = TenNgauNhien();
        var emailMoi = EmailNgauNhien();
        var cccdMoi = CccdNgauNhien();
        var sua = await _guests.UpdateAsync(khach.Id, tenMoi, emailMoi, khach.PhoneNumber,
            cccdMoi, GuestTag.None, null);

        Assert.True(sua.Ok, sua.Message);

        var docLai = await _guests.FindExactAsync(cccdMoi);
        Assert.NotNull(docLai);
        Assert.Equal(khach.Id, docLai!.Id);
        Assert.Equal(tenMoi, docLai.FullName);
        Assert.Equal(emailMoi, docLai.Email);

        // Chieu nguoc lai: email sai dinh dang phai bi chan, khong duoc ghi de len ho so.
        var loi = await _guests.UpdateAsync(khach.Id, tenMoi, "email-sai-dinh-dang", khach.PhoneNumber,
            cccdMoi, GuestTag.None, null);
        Assert.False(loi.Ok);
        Assert.NotEmpty(loi.Message);
    }

    [DbFact]
    public async Task XoaKhach_ChuaCoBooking_XoaDuocVaBienMatKhoiDb()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();

        var xoa = await _guests.DeleteAsync(khach.Id);

        Assert.True(xoa.Ok, xoa.Message);
        Assert.Null(await _guests.FindExactAsync(khach.PhoneNumber));
    }

    [DbFact]
    public async Task XoaKhach_DaCoBooking_BiChanKemLyDo()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();
        var nhan = MocXa.AddDays(10);
        var tra = nhan.AddDays(2);
        var phong = await ChonPhongTrongAsync(nhan, tra, 1);

        var dat = await _reservations.CreateAsync(khach.Id, phong.Id, 1, nhan, tra, null, null, null);
        Assert.True(dat.Ok, dat.Message);

        var xoa = await _guests.DeleteAsync(khach.Id);

        Assert.False(xoa.Ok);
        Assert.Contains("đặt phòng", xoa.Message);
        // Ho so van con nguyen - chan xoa chu khong xoa nua chung.
        Assert.NotNull(await _guests.FindExactAsync(khach.PhoneNumber));
    }

    [DbFact]
    public async Task TaoKhach_TrungSoGiayTo_BiChan_ConTrungSdtThiVanChoPhep()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();

        // Chieu bi chan: CCCD la khoa duy nhat (unique index) nen khong duoc trung.
        var trungCccd = await _guests.CreateAsync(TenNgauNhien(), EmailNgauNhien(), SdtNgauNhien(),
            khach.IdentityNumber, GuestTag.None, null);
        Assert.False(trungCccd.Ok);
        Assert.Contains("giấy tờ", trungCccd.Message);

        // Chieu hop le: he thong hien KHONG chan trung so dien thoai (xem notes).
        var trungSdt = await _guests.CreateAsync(TenNgauNhien(), EmailNgauNhien(), khach.PhoneNumber,
            CccdNgauNhien(), GuestTag.None, null);
        Assert.True(trungSdt.Ok, trungSdt.Message);
        GhiNhoDeDonDep(trungSdt.Data!.Id);
    }

    [DbFact]
    public async Task DoiTagKhach_VipRoiBlacklisted_LuuKemGhiChuVaDocLaiDung()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();

        var vip = await _guests.UpdateAsync(khach.Id, khach.FullName, khach.Email, khach.PhoneNumber,
            khach.IdentityNumber, GuestTag.Vip, "Khach quen, uu tien nhan phong som");
        Assert.True(vip.Ok, vip.Message);

        var sauVip = await _guests.FindExactAsync(khach.PhoneNumber);
        Assert.Equal(GuestTag.Vip, sauVip!.Tag);
        Assert.Equal("Khach quen, uu tien nhan phong som", sauVip.TagNote);

        var chanKhach = await _guests.UpdateAsync(khach.Id, khach.FullName, khach.Email, khach.PhoneNumber,
            khach.IdentityNumber, GuestTag.Blacklisted, "Gay on lon, tu choi thanh toan");
        Assert.True(chanKhach.Ok, chanKhach.Message);

        var sauChan = await _guests.FindExactAsync(khach.PhoneNumber);
        Assert.Equal(GuestTag.Blacklisted, sauChan!.Tag);
        Assert.Equal("Gay on lon, tu choi thanh toan", sauChan.TagNote);

        // Ve None thi ghi chu phai bi don sach - tranh de lai ghi chu "mo coi" gay hieu nham.
        var veNone = await _guests.UpdateAsync(khach.Id, khach.FullName, khach.Email, khach.PhoneNumber,
            khach.IdentityNumber, GuestTag.None, "ghi chu thua");
        Assert.True(veNone.Ok, veNone.Message);
        Assert.Null((await _guests.FindExactAsync(khach.PhoneNumber))!.TagNote);
    }

    // ------------------------------------------------------- TAI KHOAN KHACH HANG

    [DbFact]
    public async Task KichHoatTaiKhoanKhach_DangNhapDungMatKhauOk_SaiMatKhauBiChan()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();
        const string matKhau = "Khach@2026";

        var kichHoat = await _accounts.ActivateAsync(khach.Id, matKhau);
        Assert.True(kichHoat.Ok, kichHoat.Message);
        Assert.Equal(khach.Id, kichHoat.Data!.GuestId);

        var dungMatKhau = await _accounts.LoginAsync(khach.PhoneNumber, matKhau);
        Assert.True(dungMatKhau.Ok, dungMatKhau.Message);
        Assert.Equal(khach.Id, dungMatKhau.Data!.GuestId);

        var saiMatKhau = await _accounts.LoginAsync(khach.PhoneNumber, "SaiMatKhau@1");
        Assert.False(saiMatKhau.Ok);
        Assert.NotEmpty(saiMatKhau.Message);
    }

    [DbFact]
    public async Task KichHoatTaiKhoanKhach_LanThuHaiChoCungKhach_BiChan()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();

        var lanDau = await _accounts.ActivateAsync(khach.Id, "Khach@2026");
        Assert.True(lanDau.Ok, lanDau.Message);

        var lanHai = await _accounts.ActivateAsync(khach.Id, "KhachKhac@2026");
        Assert.False(lanHai.Ok);
        Assert.Contains("tài khoản", lanHai.Message);
    }

    [DbFact]
    public async Task KichHoatTaiKhoanKhach_MatKhauYeu_BiChanVaKhongTaoTaiKhoan()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();

        var yeu = await _accounts.ActivateAsync(khach.Id, "123");

        Assert.False(yeu.Ok);
        Assert.NotEmpty(yeu.Message);
        // Khong duoc tao ban ghi nao: dang nhap sau do phai that bai.
        var dangNhap = await _accounts.LoginAsync(khach.PhoneNumber, "123");
        Assert.False(dangNhap.Ok);
    }

    // ------------------------------------------------------------------ DAT PHONG

    [DbFact]
    public async Task DatPhong_CoTienCoc_LuuDungSoTien_ThieuPhuongThucThiBiChan()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();
        var nhan = MocXa.AddDays(20);
        var tra = nhan.AddDays(3);
        var phong = await ChonPhongTrongAsync(nhan, tra, 1);

        var dat = await _reservations.CreateAsync(khach.Id, phong.Id, 1, nhan, tra,
            "Xin phong tang cao", 500_000m, PaymentMethod.Cash);

        Assert.True(dat.Ok, dat.Message);
        GhiNhoDeDonDep(khach.Id);

        // Doc lai tu DB de chac chan tien coc da duoc ghi xuong, khong chi nam trong object tra ve.
        var tuDb = (await _reservations.GetAllAsync()).Single(r => r.Id == dat.Data!.Id);
        Assert.Equal(500_000m, tuDb.DepositAmount);
        Assert.Equal(PaymentMethod.Cash, tuDb.DepositPaymentMethod);
        Assert.NotNull(tuDb.DepositPaidAt);
        Assert.Equal(ReservationStatus.Pending, tuDb.Status);
        Assert.Equal(khach.Id, tuDb.GuestId);

        // Chieu bi chan: co tien coc ma khong chon phuong thuc thanh toan.
        var thieuPhuongThuc = await _reservations.CreateAsync(khach.Id, phong.Id, 1,
            nhan.AddDays(10), nhan.AddDays(12), null, 300_000m, null);
        Assert.False(thieuPhuongThuc.Ok);
        Assert.Contains("phương thức", thieuPhuongThuc.Message);
    }

    [DbFact]
    public async Task DatPhong_TrungLichCungMotPhong_BiChan()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();
        var nhan = MocXa.AddDays(40);
        var tra = nhan.AddDays(2);
        // Lay phong trong cho ca khoang rong hon vi test se dat them mot don noi tiep phia sau.
        var phong = await ChonPhongTrongAsync(nhan, tra.AddDays(3), 1);

        var don1 = await _reservations.CreateAsync(khach.Id, phong.Id, 1, nhan, tra, null, null, null);
        Assert.True(don1.Ok, don1.Message);

        // Gio lan vao nua khoang cua don1 -> phai bi chan.
        var don2 = await _reservations.CreateAsync(khach.Id, phong.Id, 1,
            nhan.AddDays(1), tra.AddDays(2), null, null, null);

        Assert.False(don2.Ok);
        Assert.Contains("trùng", don2.Message);

        // Khoang khong giao nhau tren cung phong thi van dat duoc.
        var don3 = await _reservations.CreateAsync(khach.Id, phong.Id, 1,
            tra.AddDays(1), tra.AddDays(3), null, null, null);
        Assert.True(don3.Ok, don3.Message);
    }

    [DbFact]
    public async Task SuaDatPhong_DoiNgayVaSoKhach_LuuDung()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();
        var nhan = MocXa.AddDays(60);
        var tra = nhan.AddDays(2);
        // Can suc chua >= 2 vi test se nang so khach len 2.
        var phong = await ChonPhongTrongAsync(nhan, nhan.AddDays(8), 2);

        var dat = await _reservations.CreateAsync(khach.Id, phong.Id, 1, nhan, tra, null, null, null);
        Assert.True(dat.Ok, dat.Message);

        var nhanMoi = nhan.AddDays(1);
        var traMoi = nhan.AddDays(5);
        var sua = await _reservations.UpdateAsync(dat.Data!.Id, phong.Id, 2, nhanMoi, traMoi, "Them giuong phu");

        Assert.True(sua.Ok, sua.Message);

        var tuDb = (await _reservations.GetAllAsync()).Single(r => r.Id == dat.Data.Id);
        Assert.Equal(2, tuDb.NumberOfGuests);
        Assert.Equal(nhanMoi, tuDb.CheckInDate);
        Assert.Equal(traMoi, tuDb.CheckOutDate);
        Assert.Equal("Them giuong phu", tuDb.SpecialRequests);

        // Chieu bi chan: ngay tra khong duoc truoc/bang ngay nhan.
        var ngaySai = await _reservations.UpdateAsync(dat.Data.Id, phong.Id, 2, traMoi, nhanMoi, null);
        Assert.False(ngaySai.Ok);
        Assert.NotEmpty(ngaySai.Message);
    }

    [DbFact]
    public async Task SuaDatPhong_DonDaCheckedIn_BiChan()
    {
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();
        var nhan = MocXa.AddDays(80);
        var tra = nhan.AddDays(2);
        var phong = await ChonPhongTrongAsync(nhan, nhan.AddDays(6), 1);

        var dat = await _reservations.CreateAsync(khach.Id, phong.Id, 1, nhan, tra, null, null, null);
        Assert.True(dat.Ok, dat.Message);

        // Day thang trang thai xuong DB thay cho luong check-in that (thuoc mang khac),
        // muc tieu chi la kiem rule "trang thai nay khong cho sua".
        await DatTrangThaiAsync(dat.Data!.Id, ReservationStatus.CheckedIn);

        var sua = await _reservations.UpdateAsync(dat.Data.Id, phong.Id, 1,
            nhan.AddDays(1), tra.AddDays(1), null);

        Assert.False(sua.Ok);
        Assert.Contains("không cho phép", sua.Message);

        // Du lieu cu phai giu nguyen.
        var tuDb = (await _reservations.GetAllAsync()).Single(r => r.Id == dat.Data.Id);
        Assert.Equal(nhan, tuDb.CheckInDate);
        Assert.Equal(tra, tuDb.CheckOutDate);
    }

    // ----------------------------------------------------------------- PHAN QUYEN

    [DbFact]
    public async Task PhanQuyen_NhanVienDichVu_KhongTaoDuocKhachVaDatPhong_LeTanThiDuoc()
    {
        // Le tan tao truoc mot khach de co du lieu doi chieu.
        await DangNhapAsync(EmailLeTan);
        var khach = await TaoKhachAsync();
        var nhan = MocXa.AddDays(100);
        var tra = nhan.AddDays(2);
        var phong = await ChonPhongTrongAsync(nhan, tra, 1);

        await DangNhapAsync(EmailNhanVienDichVu);

        var taoKhach = await _guests.CreateAsync(TenNgauNhien(), null, SdtNgauNhien(), null, GuestTag.None, null);
        // Neu (bat ngo) tao duoc thi van phai ghi so de don - khong thi assert fail se de lai rac trong DB
        if (taoKhach.Ok && taoKhach.Data != null) GhiNhoDeDonDep(taoKhach.Data.Id);
        Assert.False(taoKhach.Ok);
        Assert.Contains("quyền", taoKhach.Message);

        var taoDon = await _reservations.CreateAsync(khach.Id, phong.Id, 1, nhan, tra, null, null, null);
        Assert.False(taoDon.Ok);
        Assert.Contains("quyền", taoDon.Message);

        var xoaKhach = await _guests.DeleteAsync(khach.Id);
        Assert.False(xoaKhach.Ok);

        // Quay lai vai tro dung thi cung thao tac do phai chay duoc.
        await DangNhapAsync(EmailLeTan);
        var taoDonLeTan = await _reservations.CreateAsync(khach.Id, phong.Id, 1, nhan, tra, null, null, null);
        Assert.True(taoDonLeTan.Ok, taoDonLeTan.Message);
    }

    // ------------------------------------------------------------------- TIEN ICH

    /// <summary>
    /// Dang nhap bang user THAT trong DB kem Include(Role) - RoleName phai co gia tri thi
    /// AuthorizationPolicy moi cho qua, va CreatedByUserId cua don dat phong moi khong vi pham FK.
    /// </summary>
    private static async Task DangNhapAsync(string email)
    {
        // Tai khoan demo (receptionist@hotel.com...) bi app khoa moi lan khoi dong nen KHONG chac ton tai
        // -> uu tien dung no neu con active, khong thi nho TestUsers tao tai khoan cung vai tro.
        await using var db = HotelDbContextFactory.Create();
        var user = await db.Users.AsNoTracking().Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (user != null) { AppSession.SignIn(user); return; }

        var roleName = email switch
        {
            EmailLeTan => "Receptionist",
            EmailNhanVienDichVu => "ServiceStaff",
            _ => "Admin",
        };
        await TestUsers.SignInAsync(roleName);
    }

    private async Task<Guest> TaoKhachAsync()
    {
        var tao = await _guests.CreateAsync(TenNgauNhien(), EmailNgauNhien(), SdtNgauNhien(),
            CccdNgauNhien(), GuestTag.None, null);
        Assert.True(tao.Ok, tao.Message);
        GhiNhoDeDonDep(tao.Data!.Id);
        return tao.Data;
    }

    /// <summary>Chon phong con trong that trong khoang ngay - tranh hardcode Id phong seed.</summary>
    private async Task<Room> ChonPhongTrongAsync(DateTime nhan, DateTime tra, int sucChuaToiThieu)
    {
        var ketQua = await _reservations.GetAvailableRoomsAsync(nhan, tra);
        Assert.True(ketQua.Ok, ketQua.Message);
        var phong = ketQua.Data!.FirstOrDefault(r => r.RoomType.Capacity >= sucChuaToiThieu);
        Assert.True(phong != null, $"Khong con phong trong suc chua >= {sucChuaToiThieu} trong khoang test.");
        return phong!;
    }

    private static async Task DatTrangThaiAsync(int reservationId, ReservationStatus trangThai)
    {
        await using var db = HotelDbContextFactory.Create();
        var entity = await db.Reservations.FirstAsync(r => r.Id == reservationId);
        entity.Status = trangThai;
        await db.SaveChangesAsync();
    }

    private void GhiNhoDeDonDep(int guestId)
    {
        if (!_guestIds.Contains(guestId)) _guestIds.Add(guestId);
    }

    private static string TenNgauNhien() => $"KH {Guid.NewGuid():N}"[..14];
    private static string EmailNgauNhien() => $"{Guid.NewGuid():N}"[..12] + "@test.local";
    // Sinh dung DINH DANG THAT: SDT 10 so bat dau bang 0, CCCD 12 so. Truoc day sinh
    // SDT 14 so va CCCD bat dau bang chu "T" - qua duoc vi luc do service chi kiem do dai.
    private static string SdtNgauNhien() => $"0{Random.Shared.NextInt64(900_000_000, 999_999_999)}";
    private static string CccdNgauNhien() => $"0{Random.Shared.NextInt64(10_000_000_000, 99_999_999_999)}";
}
