using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

/// <summary>
/// Giam gia TU NHAP tren hoa don: quan ly go thang so tien, khong qua ma khuyen mai.
/// Khoa lai bon dieu de sau nay khong ai noi long: chi quan ly duoc dung, tru dung so,
/// khong bao gio lam tong am, va khong nhan so am.
///
/// Kem mot test cho o "Khuyen mai" o man Hoa don: truoc day danh sach xo ra RONG TRON
/// vi buoc seed khuyen mai nam sau chot chan "da co khach thi thoat" cua seed van hanh,
/// nen tren may da co du lieu no khong bao gio chay.
/// </summary>
[Collection("Db")]
public class ManualDiscountTests
{
    private const decimal BasePrice = 1_000_000m;

    [DbFact]
    public async Task QuanLy_GiamTay_TruDungSoTien_VaLuuRiengKhoiKhuyenMai()
    {
        await using var box = await Box.CreateAsync(BasePrice);
        try
        {
            await TestUsers.SignInAsync(RoleNames.Manager);
            var stayId = await box.CheckInAsync();
            await TestUsers.SignInAsync(RoleNames.Manager);

            var invoice = await new InvoiceService().ApplyApprovedDiscountAsync(stayId, 150_000m);

            Assert.True(invoice.Ok, invoice.Message);
            Assert.Equal(150_000m, invoice.Data!.ManualDiscountAmount);
            Assert.Equal(150_000m, invoice.Data.DiscountAmount);
            Assert.Equal(BasePrice - 150_000m, invoice.Data.TotalAmount);
            Assert.Null(invoice.Data.PromotionId);
            Assert.Null(invoice.Data.PromotionCode);
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task SauKhiDuyetGiamTay_TinhLaiHoaDon_VanGiuSoTienDaDuyet()
    {
        await using var box = await Box.CreateAsync(BasePrice);
        try
        {
            await TestUsers.SignInAsync(RoleNames.Manager);
            var stayId = await box.CheckInAsync();
            await TestUsers.SignInAsync(RoleNames.Manager);

            var approved = await new InvoiceService()
                .ApplyApprovedDiscountAsync(stayId, 100_000m);

            Assert.True(approved.Ok, approved.Message);
            Assert.Equal(100_000m, approved.Data!.ManualDiscountAmount);

            await TestUsers.SignInAsync(RoleNames.Receptionist);
            var recalculated = await new InvoiceService().PrepareAsync(
                stayId,
                promotionCode: null,
                asOf: DateTime.Today.AddDays(1).AddHours(11));

            Assert.True(recalculated.Ok, recalculated.Message);
            Assert.Equal(100_000m, recalculated.Data!.ManualDiscountAmount);
            Assert.Equal(100_000m, recalculated.Data.DiscountAmount);
            Assert.Equal(BasePrice - 100_000m, recalculated.Data.TotalAmount);
            Assert.Null(recalculated.Data.PromotionCode);
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task GiamTay_DuocDuyet_KhongLamMatMaKhuyenMaiThat()
    {
        await using var box = await Box.CreateAsync(BasePrice);
        try
        {
            await TestUsers.SignInAsync(RoleNames.Manager);
            var code = $"MD{Guid.NewGuid():N}"[..10].ToUpperInvariant();
            var promotion = await new PromotionService().SaveAsync(
                null,
                code,
                "Test giảm tay và khuyến mãi",
                PromotionType.Percentage,
                10m,
                DateTime.Today.AddDays(-1),
                DateTime.Today.AddDays(7),
                true);

            Assert.True(promotion.Ok, promotion.Message);
            box.PromotionId = promotion.Data!.Id;

            await TestUsers.SignInAsync(RoleNames.Receptionist);
            var stayId = await box.CheckInAsync();

            var prepared = await new InvoiceService().PrepareAsync(
                stayId,
                code,
                DateTime.Today.AddDays(1).AddHours(11));

            Assert.True(prepared.Ok, prepared.Message);

            await TestUsers.SignInAsync(RoleNames.Manager);
            var approved = await new InvoiceService()
                .ApplyApprovedDiscountAsync(stayId, 100_000m);

            Assert.True(approved.Ok, approved.Message);
            Assert.Equal(promotion.Data.Id, approved.Data!.PromotionId);
            Assert.Equal(code, approved.Data.PromotionCode);
            Assert.Equal(100_000m, approved.Data.ManualDiscountAmount);
            Assert.Equal(200_000m, approved.Data.DiscountAmount);
            Assert.Equal(800_000m, approved.Data.TotalAmount);

            await TestUsers.SignInAsync(RoleNames.Receptionist);
            var recalculated = await new InvoiceService().PrepareAsync(
                stayId,
                code,
                DateTime.Today.AddDays(1).AddHours(11));

            Assert.True(recalculated.Ok, recalculated.Message);
            Assert.Equal(promotion.Data.Id, recalculated.Data!.PromotionId);
            Assert.Equal(code, recalculated.Data.PromotionCode);
            Assert.Equal(100_000m, recalculated.Data.ManualDiscountAmount);
            Assert.Equal(800_000m, recalculated.Data.TotalAmount);
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task LeTan_KhongDuocGiamTay()
    {
        await using var box = await Box.CreateAsync(BasePrice);
        try
        {
            await TestUsers.SignInAsync(RoleNames.Receptionist);
            var stayId = await box.CheckInAsync();

            await TestUsers.SignInAsync(RoleNames.Receptionist);
            var invoice = await new InvoiceService().PrepareAsync(
                stayId, null, DateTime.Today.AddDays(1).AddHours(11), manualDiscount: 150_000m);

            Assert.False(invoice.Ok);
            Assert.False(string.IsNullOrWhiteSpace(invoice.Message));

            // Va quan trong hon: khong duoc am tham lap hoa don nao ca
            await using var db = HotelDbContextFactory.Create();
            Assert.False(await db.Invoices.AnyAsync(i => i.StayId == stayId));
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task GiamTay_LonHonTongTien_ChiGiamToiDaBangTong()
    {
        await using var box = await Box.CreateAsync(BasePrice);
        try
        {
            await TestUsers.SignInAsync(RoleNames.Manager);
            var stayId = await box.CheckInAsync();
            await TestUsers.SignInAsync(RoleNames.Manager);

            var invoice = await new InvoiceService().ApplyApprovedDiscountAsync(stayId, 9_000_000m);

            Assert.True(invoice.Ok, invoice.Message);
            // Tru toi da bang tong, khong bao gio ra so am
            Assert.Equal(BasePrice, invoice.Data!.ManualDiscountAmount);
            Assert.Equal(BasePrice, invoice.Data.DiscountAmount);
            Assert.Equal(0m, invoice.Data.TotalAmount);
            Assert.True(invoice.Data.TotalAmount >= 0);
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task GiamTay_SoAm_BiChan()
    {
        await using var box = await Box.CreateAsync(BasePrice);
        try
        {
            await TestUsers.SignInAsync(RoleNames.Manager);
            var stayId = await box.CheckInAsync();
            await TestUsers.SignInAsync(RoleNames.Manager);

            // So am se lam TANG tien phai tra - phai chan tu dau
            var invoice = await new InvoiceService().ApplyApprovedDiscountAsync(stayId, -500_000m);

            Assert.False(invoice.Ok);
        }
        finally { AppSession.SignOut(); }
    }

    /// <summary>
    /// O "Khuyen mai" o man Hoa don lay thang tu PromotionService. Danh sach rong thi le tan
    /// khong co gi de chon - dung loi nguoi dung gap. Test doi phai co it nhat MOT ma dung
    /// duoc HOM NAY (con hieu luc + trong khoang ngay), khong chi la co dong nao do trong bang.
    /// </summary>
    [DbFact]
    public async Task ManHoaDon_CoItNhatMotMaKhuyenMaiDungDuocHomNay()
    {
        await TestUsers.SignInAsync(RoleNames.Manager);
        try
        {
            // Goi lai seed: buoc khuyen mai phai chay duoc KE CA khi database da co khach
            await DemoDataDAO.SeedAsync();

            var all = await new PromotionService().GetAllAsync();
            var today = DateTime.Today;
            var dungDuoc = all.Where(p => p.IsActive
                                          && p.StartDate.Date <= today
                                          && p.EndDate.Date >= today).ToList();

            Assert.True(dungDuoc.Count > 0,
                "Khong co ma khuyen mai nao dung duoc hom nay - o Khuyen mai ben man Hoa don se rong.");
        }
        finally { AppSession.SignOut(); }
    }

    // ---------------------------------------------------------------- Tien ich

    /// <summary>Loai phong + phong + khach rieng cho test, xoa sach khi xong.</summary>
    private sealed class Box : IAsyncDisposable
    {
        public int RoomTypeId { get; private init; }
        public int RoomId { get; private init; }
        public int? PromotionId { get; set; }
        private readonly List<int> _guestIds = [];

        public static async Task<Box> CreateAsync(decimal basePrice)
        {
            await using var db = HotelDbContextFactory.Create();
            var type = new RoomType
            {
                TypeName = $"MT{Guid.NewGuid():N}"[..14],
                Capacity = 4,
                BasePrice = basePrice,
                IsActive = true,
            };
            db.RoomTypes.Add(type);
            await db.SaveChangesAsync();

            var room = new Room
            {
                RoomNumber = $"M{Guid.NewGuid():N}"[..9],
                Floor = 97,
                RoomTypeId = type.Id,
                Status = RoomStatus.Available,
                IsActive = true,
            };
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            return new Box { RoomTypeId = type.Id, RoomId = room.Id };
        }

        /// <summary>Khach thuong (khong VIP, de so tien giam chi den tu giam tay) -> dat 1 dem -> check-in.</summary>
        public async Task<int> CheckInAsync()
        {
            await TestUsers.SignInAsync(RoleNames.Receptionist);
            var suffix = $"tay{Guid.NewGuid():N}"[..12];
            var guest = await new GuestService().CreateAsync(
                $"Khach {suffix}", $"{suffix}@test.local",
                $"0{Random.Shared.NextInt64(900_000_000, 999_999_999)}",
                $"0{Random.Shared.NextInt64(10_000_000_000, 99_999_999_999)}", GuestTag.None, null);
            Assert.True(guest.Ok, guest.Message);
            _guestIds.Add(guest.Data!.Id);

            var booking = await new ReservationService().CreateAsync(
                guest.Data.Id, RoomId, 1, DateTime.Today, DateTime.Today.AddDays(1), null, null, null);
            Assert.True(booking.Ok, booking.Message);
            var confirm = await new ReservationService().ConfirmAsync(booking.Data!.Id);
            Assert.True(confirm.Ok, confirm.Message);
            var checkIn = await new StayService().CheckInAsync(booking.Data.Id, DateTime.Today.AddHours(14));
            Assert.True(checkIn.Ok, checkIn.Message);
            return checkIn.Data!.Id;
        }

        public async ValueTask DisposeAsync()
        {
            var guestIds = _guestIds;
            var roomId = RoomId;
            var typeId = RoomTypeId;
            var promotionId = PromotionId;

            await using var db = HotelDbContextFactory.Create();
            // Xoa nguoc theo thu tu khoa ngoai
            var stayIds = await db.Stays.Where(s => guestIds.Contains(s.Reservation.GuestId)).Select(s => s.Id).ToListAsync();
            var invoiceIds = await db.Invoices.Where(i => stayIds.Contains(i.StayId)).Select(i => i.Id).ToListAsync();
            await db.Payments.Where(p => invoiceIds.Contains(p.InvoiceId)).ExecuteDeleteAsync();
            await db.Invoices.Where(i => invoiceIds.Contains(i.Id)).ExecuteDeleteAsync();
            await db.Surcharges.Where(s => stayIds.Contains(s.StayId)).ExecuteDeleteAsync();
            await db.Stays.Where(s => stayIds.Contains(s.Id)).ExecuteDeleteAsync();
            await db.Reservations.Where(r => guestIds.Contains(r.GuestId)).ExecuteDeleteAsync();
            await db.Guests.Where(g => guestIds.Contains(g.Id)).ExecuteDeleteAsync();
            await db.Rooms.Where(r => r.Id == roomId).ExecuteDeleteAsync();
            await db.RoomTypes.Where(t => t.Id == typeId).ExecuteDeleteAsync();
            if (promotionId.HasValue)
            {
                await db.Promotions
                    .Where(promotion => promotion.Id == promotionId.Value)
                    .ExecuteDeleteAsync();
            }
        }
    }
}
