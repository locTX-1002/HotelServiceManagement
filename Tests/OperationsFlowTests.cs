using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Services;

namespace HotelManagement.Tests;

/// <summary>
/// Test tich hop chay THAT tren SQL Server (FUHotelManagementDB): don dich vu, yeu cau
/// buong phong va bao cao van hanh. Moi test tu tao stay rieng (guest + reservation + stay)
/// roi xoa sach o cuoi nen khong dung toi 8 khach / 11 don du lieu demo co san.
/// </summary>
[Collection("Db")]
public class OperationsFlowTests
{
    // ================= TIEN ICH DUNG CHUNG =================

    private static string Suffix() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// Dang nhap bang user THAT trong DB (co Include(Role) de AppSession.RoleName dung).
    /// Bat buoc dung user that vi service ghi CreatedByUserId/HandledByUserId - user gia
    /// se lam vi pham khoa ngoai luc SaveChanges.
    /// </summary>
    private static Task SignInAsync(string roleName) => TestUsers.SignInAsync(roleName);

    /// <summary>Tu dong dang xuat khi test ket thuc de phien khong ro ri sang test khac.</summary>
    private sealed class SessionGuard : IDisposable
    {
        public void Dispose() => AppSession.SignOut();
    }

    private static async Task<List<ServiceItem>> AvailableItemsAsync()
    {
        var items = await new ServiceCatalogRepository().GetItemsAsync(true);
        Assert.True(items.Count >= 2, "Can it nhat 2 dich vu dang ban trong DB de chay test.");
        return items;
    }

    // ================= FIXTURE: STAY TU TAO, TU XOA =================

    /// <summary>
    /// Mot ky luu tru rieng cua test: guest + reservation + stay moi tinh. Ngay dat o nam
    /// 2014 de khong dung lich cua du lieu demo (demo tinh quanh ngay hien tai).
    /// </summary>
    private sealed class StayFixture : IAsyncDisposable
    {
        public int StayId { get; private init; }
        public int ReservationId { get; private init; }
        public int GuestId { get; private init; }

        public static async Task<StayFixture> CreateAsync(StayStatus status = StayStatus.Active)
        {
            var suffix = Suffix();
            await using var db = HotelDbContextFactory.Create();

            var roomId = await db.Rooms.Where(r => r.IsActive).Select(r => r.Id).FirstOrDefaultAsync();
            Assert.True(roomId > 0, "DB khong co phong dang hoat dong de gan test.");

            var guest = new Guest
            {
                FullName = $"Test Khach {suffix}",
                Email = $"{suffix}@test.local",
                PhoneNumber = "09" + Random.Shared.Next(10_000_000, 99_999_999),
                IdentityNumber = suffix,
            };
            db.Guests.Add(guest);
            await db.SaveChangesAsync();

            var checkIn = new DateTime(2014, 1, 1);
            var reservation = new Reservation
            {
                BookingCode = $"TEST-{suffix}",
                GuestId = guest.Id,
                RoomId = roomId,
                NumberOfGuests = 1,
                CheckInDate = checkIn,
                CheckOutDate = checkIn.AddDays(1),
                Status = ReservationStatus.CheckedIn,
            };
            db.Reservations.Add(reservation);
            await db.SaveChangesAsync();

            var stay = new Stay
            {
                ReservationId = reservation.Id,
                ActualCheckIn = checkIn.AddHours(14),
                ActualCheckOut = status == StayStatus.Active ? null : checkIn.AddDays(1).AddHours(11),
                Status = status,
            };
            db.Stays.Add(stay);
            await db.SaveChangesAsync();

            return new StayFixture
            {
                StayId = stay.Id,
                ReservationId = reservation.Id,
                GuestId = guest.Id,
            };
        }

        /// <summary>Ghi thang hoa don + tien thu vao DB de co so lieu cho bao cao doanh thu.</summary>
        public async Task AddPaidInvoiceAsync(DateTime when, decimal room, decimal service,
            decimal surcharge, decimal discount, decimal collected)
        {
            await using var db = HotelDbContextFactory.Create();
            var invoice = new Invoice
            {
                StayId = StayId,
                InvoiceDate = when,
                RoomCharge = room,
                ServiceCharge = service,
                SurchargeAmount = surcharge,
                DiscountAmount = discount,
                TotalAmount = room + service + surcharge - discount,
                Status = InvoiceStatus.Paid,
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            db.Payments.Add(new Payment
            {
                InvoiceId = invoice.Id,
                PaymentDate = when,
                Amount = collected,
                PaymentMethod = PaymentMethod.Cash,
                Status = PaymentStatus.Completed,
            });
            await db.SaveChangesAsync();
        }

        // Xoa nguoc tu con len cha vi moi khoa ngoai deu dat OnDelete(Restrict).
        public async ValueTask DisposeAsync()
        {
            await using var db = HotelDbContextFactory.Create();

            var orderIds = await db.ServiceOrders.Where(o => o.StayId == StayId)
                .Select(o => o.Id).ToListAsync();
            if (orderIds.Count > 0)
            {
                db.ServiceOrderDetails.RemoveRange(
                    await db.ServiceOrderDetails.Where(d => orderIds.Contains(d.ServiceOrderId)).ToListAsync());
                await db.SaveChangesAsync();
                db.ServiceOrders.RemoveRange(
                    await db.ServiceOrders.Where(o => o.StayId == StayId).ToListAsync());
                await db.SaveChangesAsync();
            }

            db.HousekeepingRequests.RemoveRange(
                await db.HousekeepingRequests.Where(h => h.StayId == StayId).ToListAsync());
            await db.SaveChangesAsync();

            var invoiceIds = await db.Invoices.Where(i => i.StayId == StayId)
                .Select(i => i.Id).ToListAsync();
            if (invoiceIds.Count > 0)
            {
                db.Payments.RemoveRange(
                    await db.Payments.Where(p => invoiceIds.Contains(p.InvoiceId)).ToListAsync());
                await db.SaveChangesAsync();
                db.Invoices.RemoveRange(
                    await db.Invoices.Where(i => i.StayId == StayId).ToListAsync());
                await db.SaveChangesAsync();
            }

            db.Stays.RemoveRange(await db.Stays.Where(s => s.Id == StayId).ToListAsync());
            await db.SaveChangesAsync();
            db.Reservations.RemoveRange(await db.Reservations.Where(r => r.Id == ReservationId).ToListAsync());
            await db.SaveChangesAsync();
            db.Guests.RemoveRange(await db.Guests.Where(g => g.Id == GuestId).ToListAsync());
            await db.SaveChangesAsync();
        }
    }

    // ================= 1. DON DICH VU: TIEN VA TRANG THAI =================

    [DbFact]
    public async Task TaoDonDichVu_ServiceTinhDungTongTien_TheoGiaTrongDb()
    {
        await using var fixture = await StayFixture.CreateAsync();
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Receptionist);

        var items = await AvailableItemsAsync();
        var expected = items[0].UnitPrice * 2 + items[1].UnitPrice * 3;
        var orders = new ServiceOrderService();

        var result = await orders.CreateAsync(fixture.StayId,
        [
            new ServiceOrderLine(items[0].Id, 2),
            new ServiceOrderLine(items[1].Id, 3),
        ]);

        Assert.True(result.Ok, result.Message);
        Assert.Equal(expected, result.Data!.TotalAmount);
        Assert.Equal(ServiceOrderStatus.Pending, result.Data.Status);

        // Doc lai tu DB de chac chan tien da duoc GHI dung, khong chi dung tren object trong bo nho
        var saved = await orders.GetByStayAsync(fixture.StayId);
        var order = Assert.Single(saved);
        Assert.Equal(expected, order.TotalAmount);
        Assert.Equal(2, order.OrderDetails.Count);
        Assert.All(order.OrderDetails, d => Assert.Equal(d.UnitPrice * d.Quantity, d.Subtotal));
        Assert.Equal(expected, order.OrderDetails.Sum(d => d.Subtotal));
    }

    [DbFact]
    public async Task DonDichVu_PendingToProcessingToCompleted_Ok_VaCompletedLaTrangThaiCuoi()
    {
        await using var fixture = await StayFixture.CreateAsync();
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Receptionist);

        var items = await AvailableItemsAsync();
        var orders = new ServiceOrderService();
        var created = await orders.CreateAsync(fixture.StayId, [new ServiceOrderLine(items[0].Id, 1)]);
        Assert.True(created.Ok, created.Message);

        await SignInAsync(RoleNames.ServiceStaff);
        var toProcessing = await orders.ChangeStatusAsync(created.Data!.Id, ServiceOrderStatus.Processing);
        Assert.True(toProcessing.Ok, toProcessing.Message);
        Assert.Equal(ServiceOrderStatus.Processing, toProcessing.Data!.Status);

        var toCompleted = await orders.ChangeStatusAsync(created.Data.Id, ServiceOrderStatus.Completed);
        Assert.True(toCompleted.Ok, toCompleted.Message);
        Assert.Equal(ServiceOrderStatus.Completed, toCompleted.Data!.Status);

        // Completed la trang thai cuoi: khong duoc quay lai hay huy nua
        var toCancelled = await orders.ChangeStatusAsync(created.Data.Id, ServiceOrderStatus.Cancelled);
        Assert.False(toCancelled.Ok);
        Assert.False(string.IsNullOrWhiteSpace(toCancelled.Message));

        var backToProcessing = await orders.ChangeStatusAsync(created.Data.Id, ServiceOrderStatus.Processing);
        Assert.False(backToProcessing.Ok);
        Assert.False(string.IsNullOrWhiteSpace(backToProcessing.Message));

        var saved = await orders.GetByStayAsync(fixture.StayId);
        Assert.Equal(ServiceOrderStatus.Completed, Assert.Single(saved).Status);
    }

    [DbFact]
    public async Task DonDichVu_HuyTuPendingVaTuProcessing_DeuOk()
    {
        await using var fixture = await StayFixture.CreateAsync();
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Receptionist);

        var items = await AvailableItemsAsync();
        var orders = new ServiceOrderService();
        var first = await orders.CreateAsync(fixture.StayId, [new ServiceOrderLine(items[0].Id, 1)]);
        var second = await orders.CreateAsync(fixture.StayId, [new ServiceOrderLine(items[1].Id, 1)]);
        Assert.True(first.Ok, first.Message);
        Assert.True(second.Ok, second.Message);

        await SignInAsync(RoleNames.ServiceStaff);

        var cancelPending = await orders.ChangeStatusAsync(first.Data!.Id, ServiceOrderStatus.Cancelled);
        Assert.True(cancelPending.Ok, cancelPending.Message);
        Assert.Equal(ServiceOrderStatus.Cancelled, cancelPending.Data!.Status);

        var toProcessing = await orders.ChangeStatusAsync(second.Data!.Id, ServiceOrderStatus.Processing);
        Assert.True(toProcessing.Ok, toProcessing.Message);
        var cancelProcessing = await orders.ChangeStatusAsync(second.Data.Id, ServiceOrderStatus.Cancelled);
        Assert.True(cancelProcessing.Ok, cancelProcessing.Message);
        Assert.Equal(ServiceOrderStatus.Cancelled, cancelProcessing.Data!.Status);
    }

    [DbFact]
    public async Task TaoDonDichVu_DuLieuKhongHopLe_DeuBiChan()
    {
        await using var active = await StayFixture.CreateAsync();
        await using var closed = await StayFixture.CreateAsync(StayStatus.Completed);
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Receptionist);

        var items = await AvailableItemsAsync();
        var orders = new ServiceOrderService();

        var empty = await orders.CreateAsync(active.StayId, []);
        Assert.False(empty.Ok);
        Assert.False(string.IsNullOrWhiteSpace(empty.Message));

        var zeroQuantity = await orders.CreateAsync(active.StayId, [new ServiceOrderLine(items[0].Id, 0)]);
        Assert.False(zeroQuantity.Ok);
        Assert.False(string.IsNullOrWhiteSpace(zeroQuantity.Message));

        var unknownItem = await orders.CreateAsync(active.StayId, [new ServiceOrderLine(int.MaxValue, 1)]);
        Assert.False(unknownItem.Ok);
        Assert.False(string.IsNullOrWhiteSpace(unknownItem.Message));

        // Khach da tra phong thi khong con goi dich vu duoc nua
        var closedStay = await orders.CreateAsync(closed.StayId, [new ServiceOrderLine(items[0].Id, 1)]);
        Assert.False(closedStay.Ok);
        Assert.False(string.IsNullOrWhiteSpace(closedStay.Message));

        // Khong don nao duoc ghi vao DB
        Assert.Empty(await orders.GetByStayAsync(active.StayId));
        Assert.Empty(await orders.GetByStayAsync(closed.StayId));
    }

    // ================= 2. PHAN QUYEN DON DICH VU =================

    [DbFact]
    public async Task PhanQuyenTaoDon_LeTanTaoDuoc_NhanVienDichVuVaKhachVangLaiBiChan()
    {
        await using var fixture = await StayFixture.CreateAsync();
        using var session = new SessionGuard();

        var items = await AvailableItemsAsync();
        var orders = new ServiceOrderService();
        var line = new ServiceOrderLine[] { new(items[0].Id, 1) };

        await SignInAsync(RoleNames.ServiceStaff);
        var byServiceStaff = await orders.CreateAsync(fixture.StayId, line);
        Assert.False(byServiceStaff.Ok);
        Assert.False(string.IsNullOrWhiteSpace(byServiceStaff.Message));

        // Chua dang nhap thi RoleName rong -> cung phai bi chan
        AppSession.SignOut();
        var byAnonymous = await orders.CreateAsync(fixture.StayId, line);
        Assert.False(byAnonymous.Ok);

        await SignInAsync(RoleNames.Receptionist);
        var byReceptionist = await orders.CreateAsync(fixture.StayId, line);
        Assert.True(byReceptionist.Ok, byReceptionist.Message);
    }

    [DbFact]
    public async Task PhanQuyenXuLyDon_NhanVienDichVuManagerAdminDuoc_LeTanBiChan()
    {
        await using var fixture = await StayFixture.CreateAsync();
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Receptionist);

        var items = await AvailableItemsAsync();
        var orders = new ServiceOrderService();
        var first = await orders.CreateAsync(fixture.StayId, [new ServiceOrderLine(items[0].Id, 1)]);
        var second = await orders.CreateAsync(fixture.StayId, [new ServiceOrderLine(items[0].Id, 2)]);
        var third = await orders.CreateAsync(fixture.StayId, [new ServiceOrderLine(items[1].Id, 1)]);
        Assert.True(first.Ok, first.Message);
        Assert.True(second.Ok, second.Message);
        Assert.True(third.Ok, third.Message);

        // Le tan tao don duoc nhung khong duoc xu ly bep/dich vu
        var byReceptionist = await orders.ChangeStatusAsync(first.Data!.Id, ServiceOrderStatus.Processing);
        Assert.False(byReceptionist.Ok);
        Assert.False(string.IsNullOrWhiteSpace(byReceptionist.Message));

        await SignInAsync(RoleNames.ServiceStaff);
        var byServiceStaff = await orders.ChangeStatusAsync(first.Data.Id, ServiceOrderStatus.Processing);
        Assert.True(byServiceStaff.Ok, byServiceStaff.Message);

        await SignInAsync(RoleNames.Manager);
        var byManager = await orders.ChangeStatusAsync(second.Data!.Id, ServiceOrderStatus.Processing);
        Assert.True(byManager.Ok, byManager.Message);

        await SignInAsync(RoleNames.Admin);
        var byAdmin = await orders.ChangeStatusAsync(third.Data!.Id, ServiceOrderStatus.Processing);
        Assert.False(byAdmin.Ok, "Admin chỉ quản trị hệ thống, không xử lý đơn dịch vụ.");
    }

    // ================= 3. YEU CAU BUONG PHONG =================

    [DbFact]
    public async Task TaoYeuCauBuongPhong_StayDangHoatDongThiOk_StayDaDongVaGhiChuQuaDaiBiChan()
    {
        await using var active = await StayFixture.CreateAsync();
        await using var closed = await StayFixture.CreateAsync(StayStatus.Completed);
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Receptionist);

        var housekeeping = new HousekeepingRequestService();

        var ok = await housekeeping.CreateAsync(active.StayId, HousekeepingRequestType.Cleaning, "Don phong buoi sang");
        Assert.True(ok.Ok, ok.Message);
        Assert.Equal(HousekeepingRequestStatus.Pending, ok.Data!.Status);
        Assert.Equal(HousekeepingRequestType.Cleaning, ok.Data.RequestType);

        var closedStay = await housekeeping.CreateAsync(closed.StayId, HousekeepingRequestType.ExtraTowels, null);
        Assert.False(closedStay.Ok);
        Assert.False(string.IsNullOrWhiteSpace(closedStay.Message));

        var longNote = await housekeeping.CreateAsync(active.StayId, HousekeepingRequestType.Other, new string('a', 301));
        Assert.False(longNote.Ok);
        Assert.False(string.IsNullOrWhiteSpace(longNote.Message));

        var badType = await housekeeping.CreateAsync(active.StayId, (HousekeepingRequestType)99, null);
        Assert.False(badType.Ok);
        Assert.False(string.IsNullOrWhiteSpace(badType.Message));

        // Chi dung 1 yeu cau hop le duoc ghi xuong DB
        var all = await housekeeping.GetAllAsync();
        Assert.Single(all, x => x.StayId == active.StayId);
        Assert.DoesNotContain(all, x => x.StayId == closed.StayId);
    }

    [DbFact]
    public async Task YeuCauBuongPhong_PendingToAcknowledgedToCompleted_Ok_VaCompletedLaTrangThaiCuoi()
    {
        await using var fixture = await StayFixture.CreateAsync();
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Receptionist);

        var housekeeping = new HousekeepingRequestService();
        var created = await housekeeping.CreateAsync(fixture.StayId, HousekeepingRequestType.Cleaning, null);
        Assert.True(created.Ok, created.Message);

        await SignInAsync(RoleNames.ServiceStaff);
        var acknowledged = await housekeeping.ChangeStatusAsync(created.Data!.Id, HousekeepingRequestStatus.Acknowledged);
        Assert.True(acknowledged.Ok, acknowledged.Message);
        Assert.Equal(HousekeepingRequestStatus.Acknowledged, acknowledged.Data!.Status);

        var completed = await housekeeping.ChangeStatusAsync(created.Data.Id, HousekeepingRequestStatus.Completed);
        Assert.True(completed.Ok, completed.Message);
        Assert.Equal(HousekeepingRequestStatus.Completed, completed.Data!.Status);
        Assert.NotNull(completed.Data.HandledAt);

        // Da hoan tat thi khong doi tiep duoc nua
        var toCancelled = await housekeeping.ChangeStatusAsync(created.Data.Id, HousekeepingRequestStatus.Cancelled);
        Assert.False(toCancelled.Ok);
        Assert.False(string.IsNullOrWhiteSpace(toCancelled.Message));

        var backToPending = await housekeeping.ChangeStatusAsync(created.Data.Id, HousekeepingRequestStatus.Pending);
        Assert.False(backToPending.Ok);
        Assert.False(string.IsNullOrWhiteSpace(backToPending.Message));
    }

    [DbFact]
    public async Task YeuCauBuongPhong_HuyTuPendingVaTuAcknowledged_DeuOk()
    {
        await using var fixture = await StayFixture.CreateAsync();
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Receptionist);

        var housekeeping = new HousekeepingRequestService();
        var first = await housekeeping.CreateAsync(fixture.StayId, HousekeepingRequestType.ExtraWater, null);
        var second = await housekeeping.CreateAsync(fixture.StayId, HousekeepingRequestType.ExtraTowels, null);
        Assert.True(first.Ok, first.Message);
        Assert.True(second.Ok, second.Message);

        await SignInAsync(RoleNames.Manager);

        var cancelPending = await housekeeping.ChangeStatusAsync(first.Data!.Id, HousekeepingRequestStatus.Cancelled);
        Assert.True(cancelPending.Ok, cancelPending.Message);

        var acknowledged = await housekeeping.ChangeStatusAsync(second.Data!.Id, HousekeepingRequestStatus.Acknowledged);
        Assert.True(acknowledged.Ok, acknowledged.Message);
        var cancelAcknowledged = await housekeeping.ChangeStatusAsync(second.Data.Id, HousekeepingRequestStatus.Cancelled);
        Assert.True(cancelAcknowledged.Ok, cancelAcknowledged.Message);
        Assert.Equal(HousekeepingRequestStatus.Cancelled, cancelAcknowledged.Data!.Status);
    }

    [DbFact]
    public async Task YeuCauBuongPhong_LocTheoTrangThai_DemDungSoLuong()
    {
        await using var fixture = await StayFixture.CreateAsync();
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Receptionist);

        var housekeeping = new HousekeepingRequestService();
        var requests = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            var created = await housekeeping.CreateAsync(fixture.StayId, HousekeepingRequestType.Cleaning, $"Yeu cau {i}");
            Assert.True(created.Ok, created.Message);
            requests.Add(created.Data!.Id);
        }

        await SignInAsync(RoleNames.ServiceStaff);
        // requests[0] giu Pending; [1] -> Acknowledged; [2] -> Completed; [3] -> Cancelled
        var toAcknowledged = await housekeeping.ChangeStatusAsync(requests[1], HousekeepingRequestStatus.Acknowledged);
        Assert.True(toAcknowledged.Ok, toAcknowledged.Message);
        var midAcknowledged = await housekeeping.ChangeStatusAsync(requests[2], HousekeepingRequestStatus.Acknowledged);
        Assert.True(midAcknowledged.Ok, midAcknowledged.Message);
        var toCompleted = await housekeeping.ChangeStatusAsync(requests[2], HousekeepingRequestStatus.Completed);
        Assert.True(toCompleted.Ok, toCompleted.Message);
        var toCancelled = await housekeeping.ChangeStatusAsync(requests[3], HousekeepingRequestStatus.Cancelled);
        Assert.True(toCancelled.Ok, toCancelled.Message);

        // Loc tren stay cua rieng test nay de khong bi du lieu khac lam nhieu so lieu
        var mine = (await housekeeping.GetAllAsync()).Where(x => x.StayId == fixture.StayId).ToList();
        Assert.Equal(4, mine.Count);
        Assert.Single(mine, x => x.Status == HousekeepingRequestStatus.Pending);
        Assert.Single(mine, x => x.Status == HousekeepingRequestStatus.Acknowledged);
        Assert.Single(mine, x => x.Status == HousekeepingRequestStatus.Completed);
        Assert.Single(mine, x => x.Status == HousekeepingRequestStatus.Cancelled);
    }

    [DbFact]
    public async Task PhanQuyenBuongPhong_LeTanKhongXuLyDuoc_NhanVienDichVuXuLyDuoc()
    {
        await using var fixture = await StayFixture.CreateAsync();
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Receptionist);

        var housekeeping = new HousekeepingRequestService();
        var created = await housekeeping.CreateAsync(fixture.StayId, HousekeepingRequestType.Cleaning, null);
        Assert.True(created.Ok, created.Message);

        // Van dang la Receptionist -> khong duoc doi trang thai
        var byReceptionist = await housekeeping.ChangeStatusAsync(created.Data!.Id, HousekeepingRequestStatus.Acknowledged);
        Assert.False(byReceptionist.Ok);
        Assert.False(string.IsNullOrWhiteSpace(byReceptionist.Message));

        await SignInAsync(RoleNames.ServiceStaff);
        var byServiceStaff = await housekeeping.ChangeStatusAsync(created.Data.Id, HousekeepingRequestStatus.Acknowledged);
        Assert.True(byServiceStaff.Ok, byServiceStaff.Message);
    }

    // ================= 4. BAO CAO =================

    /// <summary>
    /// Chon mot khoang ngay ngau nhien trong qua khu xa (2010-2015) de hoa don cua test
    /// nam mot minh trong khoang do - bao cao ra so lieu chinh xac, khong lan voi du lieu demo.
    /// </summary>
    private static DateTime RandomReportDay()
        => new DateTime(2010, 1, 1).AddDays(Random.Shared.Next(0, 2000));

    [DbFact]
    public async Task BaoCaoDoanhThu_TongKhopChiTietTheoNgay_VaSapXepGiamDan()
    {
        await using var first = await StayFixture.CreateAsync();
        await using var second = await StayFixture.CreateAsync();
        using var session = new SessionGuard();

        var dayOne = RandomReportDay();
        var dayTwo = dayOne.AddDays(1);
        await first.AddPaidInvoiceAsync(dayOne.AddHours(10), 1_000_000, 200_000, 50_000, 100_000, 900_000);
        await second.AddPaidInvoiceAsync(dayTwo.AddHours(9), 2_000_000, 300_000, 0, 0, 2_300_000);

        await SignInAsync(RoleNames.Manager);
        var report = await new ReportService().GetRevenueAsync(dayOne, dayTwo);

        Assert.True(report.Ok, report.Message);
        var data = report.Data!;

        Assert.Equal(3_000_000, data.RoomRevenue);
        Assert.Equal(500_000, data.ServiceRevenue);
        Assert.Equal(50_000, data.SurchargeRevenue);
        Assert.Equal(100_000, data.DiscountAmount);
        Assert.Equal(3_200_000, data.CollectedAmount);

        // Doanh thu hoa don = phong + dich vu + phu thu - giam gia
        Assert.Equal(data.RoomRevenue + data.ServiceRevenue + data.SurchargeRevenue - data.DiscountAmount,
            data.InvoiceRevenue);

        Assert.True(data.RoomRevenue >= 0 && data.ServiceRevenue >= 0 && data.SurchargeRevenue >= 0
            && data.DiscountAmount >= 0 && data.InvoiceRevenue >= 0 && data.CollectedAmount >= 0,
            "Bao cao khong duoc co so am.");

        // Tong phai bang tong cong don theo tung ngay
        Assert.Equal(2, data.ByDay.Count);
        Assert.Equal(data.RoomRevenue, data.ByDay.Sum(x => x.RoomRevenue));
        Assert.Equal(data.ServiceRevenue, data.ByDay.Sum(x => x.ServiceRevenue));
        Assert.Equal(data.InvoiceRevenue, data.ByDay.Sum(x => x.InvoiceRevenue));
        Assert.Equal(data.CollectedAmount, data.ByDay.Sum(x => x.CollectedAmount));

        // De bai doi sap xep giam dan theo DOANH THU (docs/PHAN_CONG.md), khong phai theo ngay
        Assert.True(data.ByDay[0].InvoiceRevenue >= data.ByDay[1].InvoiceRevenue,
            "Bang phai sap xep giam dan theo doanh thu.");
        // Van du ca hai ngay trong khoang, khong bo ngay nao
        Assert.Contains(data.ByDay, x => x.Date == dayOne.Date);
        Assert.Contains(data.ByDay, x => x.Date == dayTwo.Date);
    }

    [DbFact]
    public async Task BaoCaoDoanhThu_NgayKetThucTruocNgayBatDau_BiChan()
    {
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Manager);

        var today = DateTime.Today;
        var report = await new ReportService().GetRevenueAsync(today, today.AddDays(-1));

        Assert.False(report.Ok);
        Assert.False(string.IsNullOrWhiteSpace(report.Message));
        Assert.Null(report.Data);
    }

    [DbFact]
    public async Task BaoCaoCongSuat_TongPhongKhopSoPhongDangHoatDongTrongDb()
    {
        using var session = new SessionGuard();
        await SignInAsync(RoleNames.Manager);

        await using var db = HotelDbContextFactory.Create();
        var activeRooms = await db.Rooms.CountAsync(r => r.IsActive);

        var report = await new ReportService().GetOccupancyAsync();

        Assert.True(report.Ok, report.Message);
        var data = report.Data!;
        Assert.Equal(activeRooms, data.TotalRooms);
        Assert.True(data.AvailableRooms + data.ReservedRooms + data.OccupiedRooms <= data.TotalRooms,
            "So phong theo tung trang thai khong duoc vuot tong so phong.");
        Assert.True(data.OccupancyRate is >= 0 and <= 100, "Ty le lap day phai nam trong 0-100.");

        var expectedRate = data.TotalRooms == 0
            ? 0
            : Math.Round((decimal)(data.OccupiedRooms + data.ReservedRooms) / data.TotalRooms * 100, 2);
        Assert.Equal(expectedRate, data.OccupancyRate);
    }

    [DbFact]
    public async Task XuatCsvDoanhThu_CoTieuDeVaDuDongDuLieuHopLe()
    {
        await using var fixture = await StayFixture.CreateAsync();
        using var session = new SessionGuard();

        var day = RandomReportDay();
        await fixture.AddPaidInvoiceAsync(day.AddHours(8), 500_000, 100_000, 0, 50_000, 550_000);

        await SignInAsync(RoleNames.Manager);
        var csv = await new ReportService().ExportRevenueCsvAsync(day, day);

        Assert.True(csv.Ok, csv.Message);
        Assert.False(string.IsNullOrWhiteSpace(csv.Data));
        Assert.Contains(",", csv.Data!);
        Assert.Contains("\r\n", csv.Data!);

        var lines = csv.Data!.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length); // 1 dong tieu de + 1 ngay co hoa don
        Assert.StartsWith("Date,", lines[0]);
        Assert.Equal(7, lines[0].Split(',').Length);

        var cells = lines[1].Split(',');
        Assert.Equal(7, cells.Length);
        Assert.Equal(day.ToString("yyyy-MM-dd"), cells[0]);
        Assert.All(cells.Skip(1), cell => Assert.True(decimal.TryParse(cell,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out _), $"O '{cell}' khong phai so."));
    }

    // ================= 5. PHAN QUYEN BAO CAO =================

    [DbFact]
    public async Task PhanQuyenBaoCao_ChiManagerXemDuoc()
    {
        using var session = new SessionGuard();
        var reports = new ReportService();
        var today = DateTime.Today;

        foreach (var role in new[] { RoleNames.Admin, RoleNames.Receptionist, RoleNames.ServiceStaff })
        {
            await SignInAsync(role);

            var revenue = await reports.GetRevenueAsync(today.AddDays(-7), today);
            Assert.False(revenue.Ok, $"{role} khong duoc phep xem bao cao doanh thu.");
            Assert.False(string.IsNullOrWhiteSpace(revenue.Message));

            var occupancy = await reports.GetOccupancyAsync();
            Assert.False(occupancy.Ok, $"{role} khong duoc phep xem bao cao cong suat.");

            var csv = await reports.ExportRevenueCsvAsync(today.AddDays(-7), today);
            Assert.False(csv.Ok, $"{role} khong duoc phep xuat CSV.");
        }

        await SignInAsync(RoleNames.Manager);
        var managerRevenue = await reports.GetRevenueAsync(today.AddDays(-7), today);
        Assert.True(managerRevenue.Ok, managerRevenue.Message);
        var managerOccupancy = await reports.GetOccupancyAsync();
        Assert.True(managerOccupancy.Ok, managerOccupancy.Message);
    }
}
