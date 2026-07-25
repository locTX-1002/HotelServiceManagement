using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

/// <summary>
/// Uu dai khach VIP: hoa don tu dong giam 10%, khong can nhap ma khuyen mai.
/// Test chay that tren SQL Server; moi test tu tao loai phong/phong/khach rieng va xoa sach.
/// </summary>
[Collection("Db")]
public class VipDiscountTests
{
    private const decimal BasePrice = 1_000_000m;

    [DbFact]
    public async Task KhachVip_HoaDonTuGiam10PhanTram()
    {
        await using var box = await Box.CreateAsync(BasePrice);
        try
        {
            await TestUsers.SignInAsync(RoleNames.Receptionist);
            var stayId = await box.CheckInAsync(GuestTag.Vip);

            var invoice = await new InvoiceService().PrepareAsync(stayId, null, DateTime.Today.AddDays(1).AddHours(11));
            Assert.True(invoice.Ok, invoice.Message);

            // 1 dem x 1.000.000 = 1.000.000 -> VIP giam 10% = 100.000 -> con 900.000
            Assert.Equal(BasePrice, invoice.Data!.RoomCharge);
            Assert.Equal(BasePrice * 0.10m, invoice.Data.DiscountAmount);
            Assert.Equal(BasePrice * 0.90m, invoice.Data.TotalAmount);
            // Ghi ro ly do giam de le tan/khach doc duoc
            Assert.Contains("VIP10", invoice.Data.PromotionCode);
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task KhachThuong_KhongDuocGiam()
    {
        await using var box = await Box.CreateAsync(BasePrice);
        try
        {
            await TestUsers.SignInAsync(RoleNames.Receptionist);
            var stayId = await box.CheckInAsync(GuestTag.None);

            var invoice = await new InvoiceService().PrepareAsync(stayId, null, DateTime.Today.AddDays(1).AddHours(11));
            Assert.True(invoice.Ok, invoice.Message);

            Assert.Equal(0m, invoice.Data!.DiscountAmount);
            Assert.Equal(BasePrice, invoice.Data.TotalAmount);
            Assert.Null(invoice.Data.PromotionCode);
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task KhachVip_CongDonVoiMaKhuyenMai_KhongVuotTongTien()
    {
        await using var box = await Box.CreateAsync(BasePrice);
        try
        {
            await TestUsers.SignInAsync(RoleNames.Manager);
            // Ma giam 20% con hieu luc trong hom nay
            var code = $"T{Guid.NewGuid():N}"[..10].ToUpperInvariant();
            var promo = await new PromotionService().SaveAsync(
                null, code, "Test cong don VIP", PromotionType.Percentage, 20m,
                DateTime.Today.AddDays(-1), DateTime.Today.AddDays(7), true);
            Assert.True(promo.Ok, promo.Message);
            box.PromotionId = promo.Data!.Id;

            await TestUsers.SignInAsync(RoleNames.Receptionist);
            var stayId = await box.CheckInAsync(GuestTag.Vip);

            var invoice = await new InvoiceService().PrepareAsync(stayId, code, DateTime.Today.AddDays(1).AddHours(11));
            Assert.True(invoice.Ok, invoice.Message);

            // 20% ma + 10% VIP = 30% cua 1.000.000 = 300.000
            Assert.Equal(BasePrice * 0.30m, invoice.Data!.DiscountAmount);
            Assert.Equal(BasePrice * 0.70m, invoice.Data.TotalAmount);
            Assert.Contains("VIP10", invoice.Data.PromotionCode);
            Assert.Contains(code, invoice.Data.PromotionCode);
            // Khong bao gio am du giam bao nhieu
            Assert.True(invoice.Data.TotalAmount >= 0);
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
                TypeName = $"VT{Guid.NewGuid():N}"[..14],
                Capacity = 4,
                BasePrice = basePrice,
                IsActive = true,
            };
            db.RoomTypes.Add(type);
            await db.SaveChangesAsync();

            var room = new Room
            {
                RoomNumber = $"V{Guid.NewGuid():N}"[..9],
                Floor = 98,
                RoomTypeId = type.Id,
                Status = RoomStatus.Available,
                IsActive = true,
            };
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            return new Box { RoomTypeId = type.Id, RoomId = room.Id };
        }

        /// <summary>Tao khach voi nhan chi dinh -> dat phong 1 dem -> xac nhan -> check-in.</summary>
        public async Task<int> CheckInAsync(GuestTag tag)
        {
            var suffix = $"vip{Guid.NewGuid():N}"[..12];
            var guest = await new GuestService().CreateAsync(
                $"Khach {suffix}", $"{suffix}@test.local", $"0{Random.Shared.NextInt64(900_000_000, 999_999_999)}",
                $"0{Random.Shared.NextInt64(10_000_000_000, 99_999_999_999)}", tag, tag == GuestTag.Vip ? "Khach than thiet" : null);
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
            var promoId = PromotionId;

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
            if (promoId.HasValue) await db.Promotions.Where(p => p.Id == promoId.Value).ExecuteDeleteAsync();
        }
    }
}
