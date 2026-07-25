using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

/// <summary>
/// Test tich hop CHAY THAT tren SQL Server (FUHotelManagementDB) cho mang
/// "Danh muc + Phan quyen": phong, loai phong, dich vu, phu thu, khuyen mai, nguoi dung.
///
/// Nguyen tac:
/// - Khong mock, khong in-memory: goi thang Service -> Repository -> DAO -> DB that.
/// - Moi test tu tao du lieu rieng (ten co hau to ngau nhien) va tu xoa sach o finally,
///   TUYET DOI khong dung vao du lieu seed (4 user, 4 loai phong, 11 phong, 8 khach...).
/// - Kiem ca 2 chieu: dung vai tro + du lieu hop le thi Ok==true; sai vai tro hoac
///   du lieu xau thi Ok==false kem thong bao.
/// </summary>
[Collection("Db")]
public class CatalogAndAuthorizationTests
{
    // ================== TIEN ICH DUNG CHUNG ==================

    /// <summary>Hau to ngau nhien de ten/email khong dung du lieu seed va khong dung nhau giua cac lan chay.</summary>
    private static string Hau() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Lay user THAT tu DB kem Include(Role) - bat buoc, vi AppSession.RoleName doc tu
    /// CurrentUser.Role.RoleName; thieu Include thi RoleName rong va moi thu deu bi chan.
    /// </summary>
    private static async Task<User> LayUserTheoVaiTroAsync(string roleName)
    {
        // Thieu vai tro nao thi TestUsers tu tao qua service that (xem TestUsers.cs)
        var user = await TestUsers.GetAsync(roleName);
        Assert.Equal(roleName, user.Role.RoleName);
        return user;
    }

    private static async Task DangNhapAsync(string roleName)
        => AppSession.SignIn(await LayUserTheoVaiTroAsync(roleName));

    /// <summary>Khang dinh 1 thao tac bi chan: Ok==false VA co thong bao cho nguoi dung doc.</summary>
    private static void ChanVoiThongBao(bool ok, string message, string boiCanh)
    {
        Assert.False(ok, $"{boiCanh}: ky vong bi chan nhung lai thanh cong.");
        Assert.False(string.IsNullOrWhiteSpace(message), $"{boiCanh}: bi chan nhung khong co thong bao loi.");
    }

    /// <summary>
    /// Gom id cua moi ban ghi test tao ra de xoa sach o finally.
    /// Xoa theo thu tu con -> cha vi khoa ngoai dat OnDelete(Restrict).
    /// </summary>
    private sealed class DonDep
    {
        public List<int> RoomIds { get; } = [];
        public List<int> RoomTypeIds { get; } = [];
        public List<int> ServiceItemIds { get; } = [];
        public List<int> ServiceCategoryIds { get; } = [];
        public List<int> SurchargeItemIds { get; } = [];
        public List<int> PromotionIds { get; } = [];
        public List<int> UserIds { get; } = [];

        public async Task ChayAsync()
        {
            // Copy ra bien cuc bo cho EF dich thanh IN (...) gon gang
            var roomIds = RoomIds;
            var roomTypeIds = RoomTypeIds;
            var serviceItemIds = ServiceItemIds;
            var serviceCategoryIds = ServiceCategoryIds;
            var surchargeItemIds = SurchargeItemIds;
            var promotionIds = PromotionIds;
            var userIds = UserIds;

            await using var db = HotelDbContextFactory.Create();
            if (roomIds.Count > 0)
                await db.Rooms.Where(x => roomIds.Contains(x.Id)).ExecuteDeleteAsync();
            if (roomTypeIds.Count > 0)
                await db.RoomTypes.Where(x => roomTypeIds.Contains(x.Id)).ExecuteDeleteAsync();
            if (serviceItemIds.Count > 0)
                await db.ServiceItems.Where(x => serviceItemIds.Contains(x.Id)).ExecuteDeleteAsync();
            if (serviceCategoryIds.Count > 0)
                await db.ServiceCategories.Where(x => serviceCategoryIds.Contains(x.Id)).ExecuteDeleteAsync();
            if (surchargeItemIds.Count > 0)
                await db.SurchargeItems.Where(x => surchargeItemIds.Contains(x.Id)).ExecuteDeleteAsync();
            if (promotionIds.Count > 0)
                await db.Promotions.Where(x => promotionIds.Contains(x.Id)).ExecuteDeleteAsync();
            if (userIds.Count > 0)
                await db.Users.Where(x => userIds.Contains(x.Id)).ExecuteDeleteAsync();
        }
    }

    // ================== 1. PHONG / LOAI PHONG ==================

    [DbFact]
    public async Task LoaiPhongVaPhong_VongDoiDayDu_ThanhCong()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Manager);
            var roomTypeService = new RoomTypeService();
            var roomService = new RoomService();
            var hau = Hau();

            // Tao loai phong moi
            var loai = await roomTypeService.CreateAsync($"TestLoai_{hau}", 3, 1_234_500.50m, "Loai phong test", true);
            Assert.True(loai.Ok, loai.Message);
            Assert.NotNull(loai.Data);
            rac.RoomTypeIds.Add(loai.Data!.Id);

            // Tao phong thuoc loai vua tao
            var soPhong = $"T{hau}";
            var phong = await roomService.CreateAsync(soPhong, 9, loai.Data.Id, RoomStatus.Available, true);
            Assert.True(phong.Ok, phong.Message);
            Assert.NotNull(phong.Data);
            rac.RoomIds.Add(phong.Data!.Id);
            var roomId = phong.Data.Id;

            // Doi trang thai van hanh: Trong -> Dang don -> Trong
            var sangDon = await roomService.UpdateStatusAsync(roomId, RoomStatus.Cleaning, true);
            Assert.True(sangDon.Ok, sangDon.Message);
            var veTrong = await roomService.UpdateStatusAsync(roomId, RoomStatus.Available, true);
            Assert.True(veTrong.Ok, veTrong.Message);

            // Tat phong (IsActive=false) - phong chua co dat phong nen duoc phep
            var tat = await roomService.UpdateAsync(roomId, soPhong, 9, loai.Data.Id, RoomStatus.Available, false);
            Assert.True(tat.Ok, tat.Message);

            await using (var db = HotelDbContextFactory.Create())
            {
                var trongDb = await db.Rooms.AsNoTracking().FirstAsync(r => r.Id == roomId);
                Assert.False(trongDb.IsActive);
                Assert.Equal(RoomStatus.Available, trongDb.Status);
            }

            // Chieu nguoc: phong da ngung dung thi khong duoc doi trang thai van hanh nua
            var doiSauKhiTat = await roomService.UpdateStatusAsync(roomId, RoomStatus.Cleaning, true);
            ChanVoiThongBao(doiSauKhiTat.Ok, doiSauKhiTat.Message, "Doi trang thai phong da ngung dung");
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    [DbFact]
    public async Task TaoPhong_TrungSoPhong_BiChan()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Admin);
            var roomTypeService = new RoomTypeService();
            var roomService = new RoomService();
            var hau = Hau();

            var loai = await roomTypeService.CreateAsync($"TestLoai_{hau}", 2, 700_000m, null, true);
            Assert.True(loai.Ok, loai.Message);
            rac.RoomTypeIds.Add(loai.Data!.Id);

            var soPhong = $"T{hau}";
            var lan1 = await roomService.CreateAsync(soPhong, 8, loai.Data.Id, RoomStatus.Available, true);
            Assert.True(lan1.Ok, lan1.Message);
            rac.RoomIds.Add(lan1.Data!.Id);

            // Cung so phong -> phai bi chan (chan truoc + unique index DB)
            var lan2 = await roomService.CreateAsync(soPhong, 8, loai.Data.Id, RoomStatus.Available, true);
            ChanVoiThongBao(lan2.Ok, lan2.Message, "Tao phong trung so phong");
            if (lan2.Data != null && lan2.Data.Id > 0)
            {
                rac.RoomIds.Add(lan2.Data.Id);   // phong ho, neu service lo tao that thi van don sach
            }

            // Phong moi khong duoc bat dau o trang thai Da dat / Dang o
            var saiTrangThai = await roomService.CreateAsync($"X{hau}", 8, loai.Data.Id, RoomStatus.Occupied, true);
            ChanVoiThongBao(saiTrangThai.Ok, saiTrangThai.Message, "Tao phong voi trang thai Dang o");
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    [DbFact]
    public async Task TaoLoaiPhong_TrungTen_BiChan()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Manager);
            var roomTypeService = new RoomTypeService();
            var ten = $"TestLoai_{Hau()}";

            var lan1 = await roomTypeService.CreateAsync(ten, 2, 500_000m, null, true);
            Assert.True(lan1.Ok, lan1.Message);
            rac.RoomTypeIds.Add(lan1.Data!.Id);

            var lan2 = await roomTypeService.CreateAsync(ten, 2, 500_000m, null, true);
            ChanVoiThongBao(lan2.Ok, lan2.Message, "Tao loai phong trung ten");
            if (lan2.Data != null && lan2.Data.Id > 0)
            {
                rac.RoomTypeIds.Add(lan2.Data.Id);
            }

            // Suc chua < 1 -> bi chan
            var sucChuaXau = await roomTypeService.CreateAsync($"TestLoai_{Hau()}", 0, 500_000m, null, true);
            ChanVoiThongBao(sucChuaXau.Ok, sucChuaXau.Message, "Tao loai phong suc chua 0");
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    [DbFact]
    public async Task PhanQuyen_Receptionist_KhongDuocQuanLyPhongVaLoaiPhong()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Receptionist);
            var hau = Hau();

            // Chup trang thai phong seed truoc khi thu, de doi chieu la test khong lam gi no
            RoomStatus trangThaiTruoc;
            await using (var db = HotelDbContextFactory.Create())
            {
                trangThaiTruoc = (await db.Rooms.AsNoTracking().FirstAsync(r => r.Id == 1)).Status;
            }

            var taoLoai = await new RoomTypeService().CreateAsync($"TestLoai_{hau}", 2, 500_000m, null, true);
            ChanVoiThongBao(taoLoai.Ok, taoLoai.Message, "Receptionist tao loai phong");

            var taoPhong = await new RoomService().CreateAsync($"T{hau}", 7, 1, RoomStatus.Available, true);
            ChanVoiThongBao(taoPhong.Ok, taoPhong.Message, "Receptionist tao phong");

            // Doi trang thai phong seed: phai bi chan NGAY o buoc kiem quyen, khong cham vao du lieu seed
            var doiTrangThai = await new RoomService().UpdateStatusAsync(1, RoomStatus.Cleaning, canManageMaintenance: true);
            ChanVoiThongBao(doiTrangThai.Ok, doiTrangThai.Message, "Receptionist doi trang thai phong");

            // Phong seed so 1 phai giu nguyen trang thai (khong bi test lam ban)
            await using (var db = HotelDbContextFactory.Create())
            {
                var phongSeed = await db.Rooms.AsNoTracking().FirstAsync(r => r.Id == 1);
                Assert.Equal(trangThaiTruoc, phongSeed.Status);
            }
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    // ================== 2. DANH MUC DICH VU ==================

    [DbFact]
    public async Task DanhMucDichVu_TaoCategoryVaItem_SuaGia_BatTat_ThanhCong()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Manager);
            var catalog = new ServiceCatalogService();
            var hau = Hau();

            // Tao danh muc
            var danhMuc = await catalog.SaveCategoryAsync(null, $"TestDM_{hau}", true);
            Assert.True(danhMuc.Ok, danhMuc.Message);
            Assert.NotNull(danhMuc.Data);
            rac.ServiceCategoryIds.Add(danhMuc.Data!.Id);

            // Tao dich vu gia decimal
            var dichVu = await catalog.SaveItemAsync(null, danhMuc.Data.Id, $"TestDV_{hau}", 123_456.78m, true);
            Assert.True(dichVu.Ok, dichVu.Message);
            Assert.NotNull(dichVu.Data);
            rac.ServiceItemIds.Add(dichVu.Data!.Id);
            var itemId = dichVu.Data.Id;

            // Sua gia + tat dich vu
            var sua = await catalog.SaveItemAsync(itemId, danhMuc.Data.Id, $"TestDV_{hau}", 99_000m, false);
            Assert.True(sua.Ok, sua.Message);

            await using (var db = HotelDbContextFactory.Create())
            {
                var itemDb = await db.ServiceItems.AsNoTracking().FirstAsync(x => x.Id == itemId);
                Assert.Equal(99_000m, itemDb.UnitPrice);
                Assert.False(itemDb.IsAvailable);
            }

            // Tat danh muc roi bat lai
            var tatDanhMuc = await catalog.SaveCategoryAsync(danhMuc.Data.Id, $"TestDM_{hau}", false);
            Assert.True(tatDanhMuc.Ok, tatDanhMuc.Message);
            var batLai = await catalog.SaveCategoryAsync(danhMuc.Data.Id, $"TestDM_{hau}", true);
            Assert.True(batLai.Ok, batLai.Message);

            await using (var db = HotelDbContextFactory.Create())
            {
                var catDb = await db.ServiceCategories.AsNoTracking().FirstAsync(x => x.Id == danhMuc.Data.Id);
                Assert.True(catDb.IsActive);
            }
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    [DbFact]
    public async Task DanhMucDichVu_DuLieuKhongHopLe_BiChan()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Admin);
            var catalog = new ServiceCatalogService();
            var hau = Hau();

            var danhMuc = await catalog.SaveCategoryAsync(null, $"TestDM_{hau}", true);
            Assert.True(danhMuc.Ok, danhMuc.Message);
            rac.ServiceCategoryIds.Add(danhMuc.Data!.Id);

            var tenRong = await catalog.SaveCategoryAsync(null, "   ", true);
            ChanVoiThongBao(tenRong.Ok, tenRong.Message, "Tao danh muc ten rong");

            var itemTenRong = await catalog.SaveItemAsync(null, danhMuc.Data.Id, "", 50_000m, true);
            ChanVoiThongBao(itemTenRong.Ok, itemTenRong.Message, "Tao dich vu ten rong");

            var giaAm = await catalog.SaveItemAsync(null, danhMuc.Data.Id, $"TestDV_{hau}", -1m, true);
            ChanVoiThongBao(giaAm.Ok, giaAm.Message, "Tao dich vu gia am");

            // Danh muc khong ton tai -> bi chan
            var khongCoDanhMuc = await catalog.SaveItemAsync(null, -999, $"TestDV_{hau}", 50_000m, true);
            ChanVoiThongBao(khongCoDanhMuc.Ok, khongCoDanhMuc.Message, "Tao dich vu voi danh muc khong ton tai");
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    [DbFact]
    public async Task PhanQuyen_ReceptionistVaServiceStaff_KhongDuocQuanLyDanhMucDichVu()
    {
        try
        {
            var catalog = new ServiceCatalogService();

            await DangNhapAsync(RoleNames.Receptionist);
            var letan = await catalog.SaveCategoryAsync(null, $"TestDM_{Hau()}", true);
            ChanVoiThongBao(letan.Ok, letan.Message, "Receptionist tao danh muc dich vu");

            await DangNhapAsync(RoleNames.ServiceStaff);
            var nvdv = await catalog.SaveItemAsync(null, 1, $"TestDV_{Hau()}", 50_000m, true);
            ChanVoiThongBao(nvdv.Ok, nvdv.Message, "ServiceStaff tao dich vu");
        }
        finally
        {
            AppSession.SignOut();
        }
    }

    // ================== 3. DANH MUC PHU THU ==================

    [DbFact]
    public async Task PhuThu_TaoVaSua_ThanhCong()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Manager);
            var surcharge = new SurchargeService();
            var hau = Hau();

            var tao = await surcharge.SaveItemAsync(null, $"TestPT_{hau}", "cai", 88_000m, true);
            Assert.True(tao.Ok, tao.Message);
            Assert.NotNull(tao.Data);
            rac.SurchargeItemIds.Add(tao.Data!.Id);
            var itemId = tao.Data.Id;

            // Sua gia + tat
            var sua = await surcharge.SaveItemAsync(itemId, $"TestPT_{hau}", "bo", 150_500m, false);
            Assert.True(sua.Ok, sua.Message);

            await using var db = HotelDbContextFactory.Create();
            var itemDb = await db.SurchargeItems.AsNoTracking().FirstAsync(x => x.Id == itemId);
            Assert.Equal(150_500m, itemDb.UnitPrice);
            Assert.Equal("bo", itemDb.Unit);
            Assert.False(itemDb.IsActive);
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    [DbFact]
    public async Task PhuThu_GiaKhongDuongHoacTenRong_BiChan()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Admin);
            var surcharge = new SurchargeService();
            var hau = Hau();

            var giaBang0 = await surcharge.SaveItemAsync(null, $"TestPT_{hau}", "cai", 0m, true);
            ChanVoiThongBao(giaBang0.Ok, giaBang0.Message, "Tao phu thu gia 0");

            var giaAm = await surcharge.SaveItemAsync(null, $"TestPT_{hau}", "cai", -5_000m, true);
            ChanVoiThongBao(giaAm.Ok, giaAm.Message, "Tao phu thu gia am");

            var tenRong = await surcharge.SaveItemAsync(null, "   ", "cai", 50_000m, true);
            ChanVoiThongBao(tenRong.Ok, tenRong.Message, "Tao phu thu ten rong");

            var donViRong = await surcharge.SaveItemAsync(null, $"TestPT_{hau}", "", 50_000m, true);
            ChanVoiThongBao(donViRong.Ok, donViRong.Message, "Tao phu thu don vi rong");

            // Chieu hop le de chac chan cac ca tren bi chan vi validate chu khong phai vi quyen
            var hopLe = await surcharge.SaveItemAsync(null, $"TestPT_{hau}", "cai", 50_000m, true);
            Assert.True(hopLe.Ok, hopLe.Message);
            rac.SurchargeItemIds.Add(hopLe.Data!.Id);
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    [DbFact]
    public async Task PhanQuyen_ReceptionistVaServiceStaff_KhongDuocQuanLyDanhMucPhuThu()
    {
        try
        {
            var surcharge = new SurchargeService();

            await DangNhapAsync(RoleNames.Receptionist);
            var letan = await surcharge.SaveItemAsync(null, $"TestPT_{Hau()}", "cai", 50_000m, true);
            ChanVoiThongBao(letan.Ok, letan.Message, "Receptionist tao danh muc phu thu");

            await DangNhapAsync(RoleNames.ServiceStaff);
            var nvdv = await surcharge.SaveItemAsync(null, $"TestPT_{Hau()}", "cai", 50_000m, true);
            ChanVoiThongBao(nvdv.Ok, nvdv.Message, "ServiceStaff tao danh muc phu thu");
        }
        finally
        {
            AppSession.SignOut();
        }
    }

    // ================== 4. KHUYEN MAI ==================

    [DbFact]
    public async Task KhuyenMai_TaoMaPhanTramVaMaTienCoDinh_ThanhCong()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Manager);
            var promotion = new PromotionService();
            var homNay = DateTime.Today;
            var hau = Hau();

            var phanTram = await promotion.SaveAsync(
                null, $"TESTPT{hau}", "Giam 10%", PromotionType.Percentage, 10m,
                homNay, homNay.AddDays(30), true);
            Assert.True(phanTram.Ok, phanTram.Message);
            Assert.NotNull(phanTram.Data);
            rac.PromotionIds.Add(phanTram.Data!.Id);

            var tienCoDinh = await promotion.SaveAsync(
                null, $"TESTTC{hau}", "Giam 200k", PromotionType.FixedAmount, 200_000m,
                homNay, homNay.AddDays(30), true);
            Assert.True(tienCoDinh.Ok, tienCoDinh.Message);
            rac.PromotionIds.Add(tienCoDinh.Data!.Id);

            // Ma duoc chuan hoa UPPERCASE khi luu
            await using var db = HotelDbContextFactory.Create();
            var maDb = await db.Promotions.AsNoTracking().FirstAsync(x => x.Id == phanTram.Data.Id);
            Assert.Equal($"TESTPT{hau}".ToUpperInvariant(), maDb.Code);
            Assert.Equal(10m, maDb.Value);
            Assert.Equal(PromotionType.Percentage, maDb.Type);
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    [DbFact]
    public async Task KhuyenMai_GiaTriKhongHopLe_BiChan()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Admin);
            var promotion = new PromotionService();
            var homNay = DateTime.Today;
            var hau = Hau();

            // % > 100 -> phai bi chan
            var quaTram = await promotion.SaveAsync(
                null, $"TESTOV{hau}", null, PromotionType.Percentage, 150m,
                homNay, homNay.AddDays(10), true);
            ChanVoiThongBao(quaTram.Ok, quaTram.Message, "Tao khuyen mai % = 150");

            // Gia tri <= 0 -> bi chan
            var khongDuong = await promotion.SaveAsync(
                null, $"TESTZE{hau}", null, PromotionType.FixedAmount, 0m,
                homNay, homNay.AddDays(10), true);
            ChanVoiThongBao(khongDuong.Ok, khongDuong.Message, "Tao khuyen mai gia tri 0");

            // Ma rong -> bi chan
            var maRong = await promotion.SaveAsync(
                null, "   ", null, PromotionType.Percentage, 10m,
                homNay, homNay.AddDays(10), true);
            ChanVoiThongBao(maRong.Ok, maRong.Message, "Tao khuyen mai ma rong");

            // Bien: % = 100 van hop le
            var dungTram = await promotion.SaveAsync(
                null, $"TEST100{hau}", null, PromotionType.Percentage, 100m,
                homNay, homNay.AddDays(10), true);
            Assert.True(dungTram.Ok, dungTram.Message);
            rac.PromotionIds.Add(dungTram.Data!.Id);

            // Trung ma -> bi chan
            var trungMa = await promotion.SaveAsync(
                null, $"TEST100{hau}", null, PromotionType.Percentage, 5m,
                homNay, homNay.AddDays(10), true);
            ChanVoiThongBao(trungMa.Ok, trungMa.Message, "Tao khuyen mai trung ma");
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    [DbFact]
    public async Task KhuyenMai_NgayKetThucTruocNgayBatDau_BiChan()
    {
        try
        {
            await DangNhapAsync(RoleNames.Manager);
            var homNay = DateTime.Today;

            var nguocNgay = await new PromotionService().SaveAsync(
                null, $"TESTDT{Hau()}", null, PromotionType.Percentage, 10m,
                homNay, homNay.AddDays(-1), true);

            ChanVoiThongBao(nguocNgay.Ok, nguocNgay.Message, "Ngay ket thuc truoc ngay bat dau");
        }
        finally
        {
            AppSession.SignOut();
        }
    }

    [DbFact]
    public async Task PhanQuyen_ServiceStaffVaReceptionist_KhongDuocSuaKhuyenMai()
    {
        try
        {
            var promotion = new PromotionService();
            var homNay = DateTime.Today;

            await DangNhapAsync(RoleNames.ServiceStaff);
            var nvdv = await promotion.SaveAsync(
                null, $"TESTNV{Hau()}", null, PromotionType.Percentage, 10m,
                homNay, homNay.AddDays(10), true);
            ChanVoiThongBao(nvdv.Ok, nvdv.Message, "ServiceStaff tao khuyen mai");

            await DangNhapAsync(RoleNames.Receptionist);
            var letan = await promotion.SaveAsync(
                null, $"TESTLT{Hau()}", null, PromotionType.Percentage, 10m,
                homNay, homNay.AddDays(10), true);
            ChanVoiThongBao(letan.Ok, letan.Message, "Receptionist tao khuyen mai");
        }
        finally
        {
            AppSession.SignOut();
        }
    }

    // ================== 5. NGUOI DUNG ==================

    [DbFact]
    public async Task NguoiDung_TaoTaiKhoanMoi_VaKhoaMoLai_ThanhCong()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Admin);
            var users = new UserManagementService();
            var hau = Hau();
            var email = $"test{hau}@hotel.test";

            var tao = await users.CreateAsync($"Test User {hau}", email, "Strong@2026", 3);
            Assert.True(tao.Ok, tao.Message);
            Assert.NotNull(tao.Data);
            rac.UserIds.Add(tao.Data!.Id);
            var userId = tao.Data.Id;

            // Khoa tai khoan vua tao
            var khoa = await users.SetActiveAsync(userId, false);
            Assert.True(khoa.Ok, khoa.Message);
            await using (var db = HotelDbContextFactory.Create())
            {
                Assert.False((await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId)).IsActive);
            }

            // Mo lai
            var mo = await users.SetActiveAsync(userId, true);
            Assert.True(mo.Ok, mo.Message);
            await using (var db = HotelDbContextFactory.Create())
            {
                var userDb = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
                Assert.True(userDb.IsActive);
                Assert.Equal(email, userDb.Email);
                Assert.Equal(3, userDb.RoleId);
                // Mat khau phai duoc bam, khong luu tho
                Assert.NotEqual("Strong@2026", userDb.PasswordHash);
            }
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    [DbFact]
    public async Task NguoiDung_MatKhauYeuHoacEmailTrung_BiChan()
    {
        var rac = new DonDep();
        try
        {
            await DangNhapAsync(RoleNames.Admin);
            var users = new UserManagementService();
            var hau = Hau();
            var email = $"test{hau}@hotel.test";

            // Mat khau yeu (thieu do dai + thieu chu hoa/ky tu dac biet) -> bi chan
            var matKhauYeu = await users.CreateAsync($"Test User {hau}", email, "abc123", 3);
            ChanVoiThongBao(matKhauYeu.Ok, matKhauYeu.Message, "Tao tai khoan mat khau yeu");

            // Mat khau du 8 ky tu nhung thieu ky tu dac biet -> van bi chan
            var thieuKyTuDacBiet = await users.CreateAsync($"Test User {hau}", email, "Abcdef123", 3);
            ChanVoiThongBao(thieuKyTuDacBiet.Ok, thieuKyTuDacBiet.Message, "Tao tai khoan thieu ky tu dac biet");

            // Email sai dinh dang -> bi chan
            var emailXau = await users.CreateAsync($"Test User {hau}", "khong-phai-email", "Strong@2026", 3);
            ChanVoiThongBao(emailXau.Ok, emailXau.Message, "Tao tai khoan email sai dinh dang");

            // Tao that thanh cong
            var tao = await users.CreateAsync($"Test User {hau}", email, "Strong@2026", 3);
            Assert.True(tao.Ok, tao.Message);
            rac.UserIds.Add(tao.Data!.Id);

            // Trung email -> bi chan
            var trungEmail = await users.CreateAsync($"Test User 2 {hau}", email.ToUpperInvariant(), "Strong@2026", 4);
            ChanVoiThongBao(trungEmail.Ok, trungEmail.Message, "Tao tai khoan trung email");
            if (trungEmail.Data != null && trungEmail.Data.Id > 0)
            {
                rac.UserIds.Add(trungEmail.Data.Id);
            }

            // Vai tro khong ton tai -> bi chan
            var vaiTroXau = await users.CreateAsync($"Test User 3 {hau}", $"test{Hau()}@hotel.test", "Strong@2026", 999);
            ChanVoiThongBao(vaiTroXau.Ok, vaiTroXau.Message, "Tao tai khoan voi vai tro khong ton tai");
        }
        finally
        {
            AppSession.SignOut();
            await rac.ChayAsync();
        }
    }

    [DbFact]
    public async Task NguoiDung_TuKhoaChinhMinh_BiChan()
    {
        try
        {
            var admin = await LayUserTheoVaiTroAsync(RoleNames.Admin);
            AppSession.SignIn(admin);

            var tuKhoa = await new UserManagementService().SetActiveAsync(admin.Id, false);
            ChanVoiThongBao(tuKhoa.Ok, tuKhoa.Message, "Admin tu khoa chinh minh");

            // Tai khoan admin phai con nguyen hoat dong trong DB
            await using var db = HotelDbContextFactory.Create();
            var adminDb = await db.Users.AsNoTracking().FirstAsync(u => u.Id == admin.Id);
            Assert.True(adminDb.IsActive, "Admin bi khoa nham - test da lam hong du lieu.");
        }
        finally
        {
            AppSession.SignOut();
        }
    }

    [DbFact]
    public async Task PhanQuyen_KhongPhaiAdmin_KhongDuocQuanLyNguoiDung()
    {
        try
        {
            var users = new UserManagementService();

            foreach (var vaiTro in new[] { RoleNames.Manager, RoleNames.Receptionist, RoleNames.ServiceStaff })
            {
                await DangNhapAsync(vaiTro);

                var xemDanhSach = await users.GetAllAsync();
                ChanVoiThongBao(xemDanhSach.Ok, xemDanhSach.Message, $"{vaiTro} xem danh sach nhan vien");

                var tao = await users.CreateAsync($"Test {vaiTro}", $"test{Hau()}@hotel.test", "Strong@2026", 3);
                ChanVoiThongBao(tao.Ok, tao.Message, $"{vaiTro} tao tai khoan");

                var khoa = await users.SetActiveAsync(2, false);
                ChanVoiThongBao(khoa.Ok, khoa.Message, $"{vaiTro} khoa tai khoan");

                var datLaiMatKhau = await users.ResetPasswordAsync(2, "Strong@2026");
                ChanVoiThongBao(datLaiMatKhau.Ok, datLaiMatKhau.Message, $"{vaiTro} dat lai mat khau nguoi khac");
            }

            // Chieu dung: Admin xem duoc danh sach
            await DangNhapAsync(RoleNames.Admin);
            var adminXem = await users.GetAllAsync();
            Assert.True(adminXem.Ok, adminXem.Message);
            Assert.NotNull(adminXem.Data);
            Assert.True(adminXem.Data!.Count >= 4, "DB phai con du 4 tai khoan seed.");
        }
        finally
        {
            AppSession.SignOut();
        }
    }

    [DbFact]
    public async Task PhanQuyen_ChuaDangNhap_MoiThaoTacDanhMucDeuBiChan()
    {
        try
        {
            AppSession.SignOut();   // khong co phien -> RoleName rong

            var phong = await new RoomService().CreateAsync($"T{Hau()}", 7, 1, RoomStatus.Available, true);
            ChanVoiThongBao(phong.Ok, phong.Message, "Chua dang nhap tao phong");

            var dichVu = await new ServiceCatalogService().SaveCategoryAsync(null, $"TestDM_{Hau()}", true);
            ChanVoiThongBao(dichVu.Ok, dichVu.Message, "Chua dang nhap tao danh muc dich vu");

            var phuThu = await new SurchargeService().SaveItemAsync(null, $"TestPT_{Hau()}", "cai", 50_000m, true);
            ChanVoiThongBao(phuThu.Ok, phuThu.Message, "Chua dang nhap tao phu thu");

            var khuyenMai = await new PromotionService().SaveAsync(
                null, $"TESTKD{Hau()}", null, PromotionType.Percentage, 10m,
                DateTime.Today, DateTime.Today.AddDays(5), true);
            ChanVoiThongBao(khuyenMai.Ok, khuyenMai.Message, "Chua dang nhap tao khuyen mai");

            var nguoiDung = await new UserManagementService().CreateAsync(
                "Test", $"test{Hau()}@hotel.test", "Strong@2026", 3);
            ChanVoiThongBao(nguoiDung.Ok, nguoiDung.Message, "Chua dang nhap tao tai khoan");
        }
        finally
        {
            AppSession.SignOut();
        }
    }
}
