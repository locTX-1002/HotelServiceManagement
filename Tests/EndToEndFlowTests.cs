using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

/// <summary>
/// Collection dung chung cho moi test cham DB that. Cac class test khac chi can gan
/// [Collection("Db")] la xunit se khong chay song song chung -> tranh 2 test cung
/// sua mot phong / mot stay hoac tranh nhau AppSession (bien tinh toan cuc).
/// </summary>
/// <summary>
/// Chay MOT LAN sau khi ca collection "Db" test xong: xoa cac tai khoan ma TestUsers
/// da tao trong luc chay, de DB tra ve dung trang thai ban dau.
/// </summary>
public sealed class TestUsersFixture : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => TestUsers.CleanupAsync();
}

[CollectionDefinition("Db", DisableParallelization = true)]
public class DbCollection : ICollectionFixture<TestUsersFixture> { }

/// <summary>
/// Test tich hop chay THAT tren SQL Server: di het luong nghiep vu tu tao khach ->
/// dat phong -> check-in -> dich vu/phu thu -> hoa don -> thanh toan -> check-out,
/// va kiem cac chot chan (thieu giay to, chua thanh toan, con don dich vu mo, trung lich).
/// Moi test tu tao loai phong + phong + khach rieng roi xoa sach o cuoi, KHONG dung
/// 8 khach / 11 don seed san cua nhom.
/// </summary>
[Collection("Db")]
public class EndToEndFlowTests
{
    private const decimal BasePrice = 500_000m;

    // ---------------------------------------------------------------- 1. Luong day du

    [DbFact]
    public async Task LuongDayDu_TuDatPhongDenCheckOut_ThanhCong()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        try
        {
            await SignInAsync("Receptionist");

            // 1) Ho so khach co CCCD -> du dieu kien check-in
            var guest = await sandbox.CreateGuestAsync(withIdentity: true);

            // 2) Dat phong co tien coc (coc se tu thanh 1 Payment khi lap hoa don)
            var checkInDate = DateTime.Today;
            var checkOutDate = DateTime.Today.AddDays(2);
            var booking = await new ReservationService().CreateAsync(
                guest.Id, sandbox.RoomId, 1, checkInDate, checkOutDate, "Xin phong yen tinh",
                200_000m, PaymentMethod.Cash);
            Assert.True(booking.Ok, booking.Message);
            Assert.Equal(ReservationStatus.Pending, booking.Data!.Status);

            // 3) Xac nhan don
            var confirm = await new ReservationService().ConfirmAsync(booking.Data.Id);
            Assert.True(confirm.Ok, confirm.Message);
            Assert.Equal(ReservationStatus.Confirmed, confirm.Data!.Status);

            // 4) Check-in
            var actualCheckIn = DateTime.Today.AddHours(14);
            var checkIn = await new StayService().CheckInAsync(booking.Data.Id, actualCheckIn);
            Assert.True(checkIn.Ok, checkIn.Message);
            var stayId = checkIn.Data!.Id;
            Assert.Equal(RoomStatus.Occupied, await RoomStatusAsync(sandbox.RoomId));

            // 5) Don dich vu: le tan tao, bep/ServiceStaff xu ly toi Completed
            //    (chi don Completed moi duoc tinh tien vao hoa don)
            var item = await FirstServiceItemAsync();
            var order = await new ServiceOrderService().CreateAsync(
                stayId, [new ServiceOrderLine(item.Id, 2)]);
            Assert.True(order.Ok, order.Message);

            await SignInAsync("ServiceStaff");
            var processing = await new ServiceOrderService()
                .ChangeStatusAsync(order.Data!.Id, ServiceOrderStatus.Processing);
            Assert.True(processing.Ok, processing.Message);
            var completed = await new ServiceOrderService()
                .ChangeStatusAsync(order.Data.Id, ServiceOrderStatus.Completed);
            Assert.True(completed.Ok, completed.Message);

            // 6) Phu thu (khach lam hong do) - le tan them
            await SignInAsync("Receptionist");
            var surchargeItem = await FirstSurchargeItemAsync();
            var surcharge = await new SurchargeService().AddToStayAsync(stayId, surchargeItem.Id, 1);
            Assert.True(surcharge.Ok, surcharge.Message);

            // 7) Lap hoa don tai thoi diem tra phong (2 dem)
            var asOf = DateTime.Today.AddDays(2).AddHours(11);
            var invoice = await new InvoiceService().PrepareAsync(stayId, null, asOf);
            Assert.True(invoice.Ok, invoice.Message);
            Assert.Equal(2 * BasePrice, invoice.Data!.RoomCharge);
            Assert.Equal(item.UnitPrice * 2, invoice.Data.ServiceCharge);
            Assert.Equal(surchargeItem.UnitPrice, invoice.Data.SurchargeAmount);
            // Coc 200k < tong tien nen hoa don moi lap phai la "tra mot phan"
            Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Data.Status);

            // 8) Thanh toan phan con lai lam 2 lan: tien mat + chuyen khoan
            var invoiceId = invoice.Data.Id;
            var summary = await new PaymentService().GetSummaryAsync(invoiceId);
            Assert.True(summary.Ok, summary.Message);
            Assert.Equal(200_000m, summary.Data!.PaidAmount);

            var remaining = summary.Data.RemainingAmount;
            var firstPart = Math.Round(remaining / 2, 2);
            var lastPart = remaining - firstPart;

            var pay1 = await new PaymentService().RecordAsync(invoiceId, firstPart, PaymentMethod.Cash, null);
            Assert.True(pay1.Ok, pay1.Message);

            var transactionId = $"TXN{Guid.NewGuid():N}"[..15];
            var pay2 = await new PaymentService().RecordAsync(
                invoiceId, lastPart, PaymentMethod.BankTransfer, transactionId);
            Assert.True(pay2.Ok, pay2.Message);

            var paidSummary = await new PaymentService().GetSummaryAsync(invoiceId);
            Assert.True(paidSummary.Ok, paidSummary.Message);
            Assert.Equal(0m, paidSummary.Data!.RemainingAmount);
            Assert.Equal(InvoiceStatus.Paid, paidSummary.Data.Invoice.Status);

            // 9) Check-out: du dieu kien (hoa don Paid, khong con don dich vu mo)
            var checkOut = await new StayService().CheckOutAsync(stayId, asOf);
            Assert.True(checkOut.Ok, checkOut.Message);
            Assert.Equal(StayStatus.Completed, checkOut.Data!.Status);

            // 10) Phong phai chuyen sang dang don de buong phong lam viec
            Assert.Equal(RoomStatus.Cleaning, await RoomStatusAsync(sandbox.RoomId));
            Assert.Equal(ReservationStatus.Completed, await ReservationStatusAsync(booking.Data.Id));
        }
        finally { AppSession.SignOut(); }
    }

    // ------------------------------------------------- 2. Chan check-in khi thieu giay to

    [DbFact]
    public async Task CheckIn_KhiKhachChuaCoCccd_BiChan()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        try
        {
            await SignInAsync("Receptionist");

            // Ho so tam: chua kip xac minh giay to
            var guest = await sandbox.CreateGuestAsync(withIdentity: false);
            Assert.Null(guest.IdentityNumber);

            var booking = await new ReservationService().CreateAsync(
                guest.Id, sandbox.RoomId, 1, DateTime.Today, DateTime.Today.AddDays(1),
                null, null, null);
            Assert.True(booking.Ok, booking.Message);
            var confirm = await new ReservationService().ConfirmAsync(booking.Data!.Id);
            Assert.True(confirm.Ok, confirm.Message);

            var checkIn = await new StayService().CheckInAsync(booking.Data.Id, DateTime.Now);

            Assert.False(checkIn.Ok);
            Assert.Contains("giay to", checkIn.Message);
            // Phong khong duoc dinh trang thai khi check-in bi chan
            Assert.Equal(RoomStatus.Available, await RoomStatusAsync(sandbox.RoomId));

            // Chieu nguoc lai: bo sung CCCD xong thi check-in duoc ngay
            var update = await new GuestService().UpdateAsync(guest.Id, guest.FullName, guest.Email,
                guest.PhoneNumber, $"CCCD{Guid.NewGuid():N}"[..15], GuestTag.None, null);
            Assert.True(update.Ok, update.Message);

            var retry = await new StayService().CheckInAsync(booking.Data.Id, DateTime.Now);
            Assert.True(retry.Ok, retry.Message);
        }
        finally { AppSession.SignOut(); }
    }

    // ------------------------------------------- 3. Chan check-out khi hoa don chua Paid

    [DbFact]
    public async Task CheckOut_KhiHoaDonChuaThanhToanDu_BiChan()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        try
        {
            await SignInAsync("Receptionist");
            var stay = await sandbox.CheckInNewGuestAsync();

            // Lap hoa don nhung khong tra dong nao (don khong co tien coc)
            var asOf = DateTime.Today.AddDays(1).AddHours(11);
            var invoice = await new InvoiceService().PrepareAsync(stay.StayId, null, asOf);
            Assert.True(invoice.Ok, invoice.Message);
            Assert.Equal(InvoiceStatus.Unpaid, invoice.Data!.Status);

            var blocked = await new StayService().CheckOutAsync(stay.StayId, asOf);
            Assert.False(blocked.Ok);
            Assert.Contains("thanh toan", blocked.Message);

            // Tra mot phan van chua du -> van bi chan
            var half = Math.Round(invoice.Data.TotalAmount / 2, 2);
            var partial = await new PaymentService().RecordAsync(invoice.Data.Id, half, PaymentMethod.Cash, null);
            Assert.True(partial.Ok, partial.Message);
            var stillBlocked = await new StayService().CheckOutAsync(stay.StayId, asOf);
            Assert.False(stillBlocked.Ok);

            // Tra not phan con lai -> check-out thong
            var rest = await new PaymentService().RecordAsync(
                invoice.Data.Id, invoice.Data.TotalAmount - half, PaymentMethod.Cash, null);
            Assert.True(rest.Ok, rest.Message);
            var ok = await new StayService().CheckOutAsync(stay.StayId, asOf);
            Assert.True(ok.Ok, ok.Message);
        }
        finally { AppSession.SignOut(); }
    }

    // -------------------------------------- 4. Chan check-out khi con don dich vu chua chot

    [DbFact]
    public async Task CheckOut_KhiConDonDichVuChuaChot_BiChan()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        try
        {
            await SignInAsync("Receptionist");
            var stay = await sandbox.CheckInNewGuestAsync();

            // Don dich vu de o trang thai Pending -> chua tinh tien, cung chua duoc phep tra phong
            var item = await FirstServiceItemAsync();
            var order = await new ServiceOrderService().CreateAsync(
                stay.StayId, [new ServiceOrderLine(item.Id, 1)]);
            Assert.True(order.Ok, order.Message);

            // Van lap va thanh toan het hoa don de chac chan loi bi chan la do don dich vu
            var asOf = DateTime.Today.AddDays(1).AddHours(11);
            var invoice = await new InvoiceService().PrepareAsync(stay.StayId, null, asOf);
            Assert.True(invoice.Ok, invoice.Message);
            var pay = await new PaymentService().RecordAsync(
                invoice.Data!.Id, invoice.Data.TotalAmount, PaymentMethod.Cash, null);
            Assert.True(pay.Ok, pay.Message);

            var blocked = await new StayService().CheckOutAsync(stay.StayId, asOf);
            Assert.False(blocked.Ok);
            Assert.Contains("dich vu", blocked.Message);

            // Huy don dich vu (ServiceStaff) roi check-out lai -> thong
            await SignInAsync("ServiceStaff");
            var cancelOrder = await new ServiceOrderService()
                .ChangeStatusAsync(order.Data!.Id, ServiceOrderStatus.Cancelled);
            Assert.True(cancelOrder.Ok, cancelOrder.Message);

            await SignInAsync("Receptionist");
            var ok = await new StayService().CheckOutAsync(stay.StayId, asOf);
            Assert.True(ok.Ok, ok.Message);
        }
        finally { AppSession.SignOut(); }
    }

    // ---------------------------------------------------------- 5. Chan dat phong trung lich

    [DbFact]
    public async Task DatPhong_TrungLichCungPhong_BiChan()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        try
        {
            await SignInAsync("Receptionist");
            var guestA = await sandbox.CreateGuestAsync(withIdentity: true);
            var guestB = await sandbox.CreateGuestAsync(withIdentity: true);

            var first = await new ReservationService().CreateAsync(
                guestA.Id, sandbox.RoomId, 1,
                DateTime.Today.AddDays(10), DateTime.Today.AddDays(14), null, null, null);
            Assert.True(first.Ok, first.Message);

            // Khoang 12-16 giao voi 10-14 -> phai bi tu choi
            var overlapped = await new ReservationService().CreateAsync(
                guestB.Id, sandbox.RoomId, 1,
                DateTime.Today.AddDays(12), DateTime.Today.AddDays(16), null, null, null);
            Assert.False(overlapped.Ok);
            Assert.Contains("trung", overlapped.Message);

            // Sat ngay tra cua don truoc (14-16) thi khong giao -> phai cho dat
            var adjacent = await new ReservationService().CreateAsync(
                guestB.Id, sandbox.RoomId, 1,
                DateTime.Today.AddDays(14), DateTime.Today.AddDays(16), null, null, null);
            Assert.True(adjacent.Ok, adjacent.Message);
        }
        finally { AppSession.SignOut(); }
    }

    // ------------------------------------------------------------------- 6. Huy dat phong

    [DbFact]
    public async Task HuyDatPhong_TuTrangThaiChoVaDaXacNhan_ThanhCong()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        try
        {
            await SignInAsync("Receptionist");
            var guest = await sandbox.CreateGuestAsync(withIdentity: true);

            // Huy don dang cho xac nhan
            var pending = await new ReservationService().CreateAsync(
                guest.Id, sandbox.RoomId, 1,
                DateTime.Today.AddDays(20), DateTime.Today.AddDays(21), null, null, null);
            Assert.True(pending.Ok, pending.Message);
            var cancelPending = await new ReservationService().CancelAsync(pending.Data!.Id);
            Assert.True(cancelPending.Ok, cancelPending.Message);
            Assert.Equal(ReservationStatus.Cancelled, await ReservationStatusAsync(pending.Data.Id));

            // Huy don da xac nhan (phong duoc tra lai lich nen dat trung khoang cu van duoc)
            var confirmed = await new ReservationService().CreateAsync(
                guest.Id, sandbox.RoomId, 1,
                DateTime.Today.AddDays(20), DateTime.Today.AddDays(21), null, null, null);
            Assert.True(confirmed.Ok, confirmed.Message);
            var confirm = await new ReservationService().ConfirmAsync(confirmed.Data!.Id);
            Assert.True(confirm.Ok, confirm.Message);
            var cancelConfirmed = await new ReservationService().CancelAsync(confirmed.Data.Id);
            Assert.True(cancelConfirmed.Ok, cancelConfirmed.Message);

            // Huy lan hai tren don da huy -> phai bi chan
            var again = await new ReservationService().CancelAsync(confirmed.Data.Id);
            Assert.False(again.Ok);
            Assert.False(string.IsNullOrWhiteSpace(again.Message));
        }
        finally { AppSession.SignOut(); }
    }

    // ------------------------------------------------------- 7. Phan quyen tren luong chinh

    [DbFact]
    public async Task CheckIn_ChiLeTanTroLenDuocPhep_ServiceStaffBiChan()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        try
        {
            await SignInAsync("Receptionist");
            var guest = await sandbox.CreateGuestAsync(withIdentity: true);
            var booking = await new ReservationService().CreateAsync(
                guest.Id, sandbox.RoomId, 1, DateTime.Today, DateTime.Today.AddDays(1),
                null, null, null);
            Assert.True(booking.Ok, booking.Message);
            var confirm = await new ReservationService().ConfirmAsync(booking.Data!.Id);
            Assert.True(confirm.Ok, confirm.Message);

            // Sai vai tro: nhan vien dich vu khong duoc check-in
            await SignInAsync("ServiceStaff");
            var denied = await new StayService().CheckInAsync(booking.Data.Id, DateTime.Now);
            Assert.False(denied.Ok);
            Assert.Contains("quyen", denied.Message);

            // Dung vai tro: le tan check-in duoc
            await SignInAsync("Receptionist");
            var allowed = await new StayService().CheckInAsync(booking.Data.Id, DateTime.Now);
            Assert.True(allowed.Ok, allowed.Message);
        }
        finally { AppSession.SignOut(); }
    }

    // ------------------------------------------------------------------------- Tien ich

    /// <summary>Dang nhap bang user THAT trong DB; thieu vai tro thi TestUsers tu tao (xem TestUsers.cs).</summary>
    private static Task SignInAsync(string roleName) => TestUsers.SignInAsync(roleName);

    private static async Task<RoomStatus> RoomStatusAsync(int roomId)
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Rooms.AsNoTracking().Where(r => r.Id == roomId)
            .Select(r => r.Status).FirstAsync();
    }

    private static async Task<ReservationStatus> ReservationStatusAsync(int reservationId)
    {
        await using var context = HotelDbContextFactory.Create();
        return await context.Reservations.AsNoTracking().Where(r => r.Id == reservationId)
            .Select(r => r.Status).FirstAsync();
    }

    private static async Task<ServiceItem> FirstServiceItemAsync()
    {
        await using var context = HotelDbContextFactory.Create();
        var item = await context.ServiceItems.AsNoTracking().Include(i => i.ServiceCategory)
            .FirstOrDefaultAsync(i => i.IsAvailable && i.ServiceCategory.IsActive);
        Assert.True(item != null, "DB chua co dich vu nao dang ban.");
        return item!;
    }

    private static async Task<SurchargeItem> FirstSurchargeItemAsync()
    {
        await using var context = HotelDbContextFactory.Create();
        var item = await context.SurchargeItems.AsNoTracking().FirstOrDefaultAsync(i => i.IsActive);
        Assert.True(item != null, "DB chua co muc phu thu nao dang bat.");
        return item!;
    }

    /// <summary>
    /// Vung du lieu rieng cua tung test: tu tao loai phong + phong moi (khong dung 11 phong
    /// seed de check-in/check-out khong lam doi trang thai phong demo cua nhom), va xoa
    /// sach moi thu da tao khi test ket thuc.
    /// </summary>
    private sealed class Sandbox : IAsyncDisposable
    {
        public int RoomTypeId { get; private init; }
        public int RoomId { get; private init; }

        private readonly List<int> _guestIds = [];

        public static async Task<Sandbox> CreateAsync()
        {
            await using var context = HotelDbContextFactory.Create();
            var roomType = new RoomType
            {
                TypeName = $"TT{Guid.NewGuid():N}"[..14],
                Capacity = 4,
                BasePrice = BasePrice,
                Description = "Loai phong tam cho test tich hop",
                IsActive = true,
            };
            context.RoomTypes.Add(roomType);
            await context.SaveChangesAsync();

            var room = new Room
            {
                RoomNumber = $"T{Guid.NewGuid():N}"[..9],
                Floor = 99,
                RoomTypeId = roomType.Id,
                Status = RoomStatus.Available,
                IsActive = true,
            };
            context.Rooms.Add(room);
            await context.SaveChangesAsync();

            return new Sandbox { RoomTypeId = roomType.Id, RoomId = room.Id };
        }

        /// <summary>Tao khach moi qua GuestService (phai dang nhap vai tro le tan tro len).</summary>
        public async Task<Guest> CreateGuestAsync(bool withIdentity)
        {
            var suffix = $"test{Guid.NewGuid():N}"[..12];
            var result = await new GuestService().CreateAsync(
                $"Khach {suffix}",
                $"{suffix}@test.local",
                $"09{Guid.NewGuid():N}"[..11],
                withIdentity ? $"CCCD{Guid.NewGuid():N}"[..15] : null,
                GuestTag.None,
                null);
            Assert.True(result.Ok, result.Message);
            _guestIds.Add(result.Data!.Id);
            return result.Data;
        }

        /// <summary>Rut gon: khach co CCCD -> dat phong 1 dem -> xac nhan -> check-in.</summary>
        public async Task<(int ReservationId, int StayId)> CheckInNewGuestAsync()
        {
            var guest = await CreateGuestAsync(withIdentity: true);
            var booking = await new ReservationService().CreateAsync(
                guest.Id, RoomId, 1, DateTime.Today, DateTime.Today.AddDays(1), null, null, null);
            Assert.True(booking.Ok, booking.Message);
            var confirm = await new ReservationService().ConfirmAsync(booking.Data!.Id);
            Assert.True(confirm.Ok, confirm.Message);
            var checkIn = await new StayService().CheckInAsync(booking.Data.Id, DateTime.Today.AddHours(14));
            Assert.True(checkIn.Ok, checkIn.Message);
            return (booking.Data.Id, checkIn.Data!.Id);
        }

        /// <summary>Xoa nguoc theo thu tu khoa ngoai: payment -> invoice -> ... -> phong -> loai phong.</summary>
        public async ValueTask DisposeAsync()
        {
            await using var context = HotelDbContextFactory.Create();

            var reservations = await context.Reservations
                .Where(r => _guestIds.Contains(r.GuestId)).ToListAsync();
            var reservationIds = reservations.Select(r => r.Id).ToList();

            var stays = await context.Stays
                .Where(s => reservationIds.Contains(s.ReservationId)).ToListAsync();
            var stayIds = stays.Select(s => s.Id).ToList();

            var invoices = await context.Invoices.Where(i => stayIds.Contains(i.StayId)).ToListAsync();
            var invoiceIds = invoices.Select(i => i.Id).ToList();
            context.Payments.RemoveRange(
                await context.Payments.Where(p => invoiceIds.Contains(p.InvoiceId)).ToListAsync());
            context.Invoices.RemoveRange(invoices);

            context.Surcharges.RemoveRange(
                await context.Surcharges.Where(x => stayIds.Contains(x.StayId)).ToListAsync());

            var orders = await context.ServiceOrders.Where(o => stayIds.Contains(o.StayId)).ToListAsync();
            var orderIds = orders.Select(o => o.Id).ToList();
            context.ServiceOrderDetails.RemoveRange(
                await context.ServiceOrderDetails.Where(d => orderIds.Contains(d.ServiceOrderId)).ToListAsync());
            context.ServiceOrders.RemoveRange(orders);
            await context.SaveChangesAsync();

            context.Stays.RemoveRange(stays);
            await context.SaveChangesAsync();

            context.Reservations.RemoveRange(reservations);
            await context.SaveChangesAsync();

            context.Guests.RemoveRange(
                await context.Guests.Where(g => _guestIds.Contains(g.Id)).ToListAsync());
            context.Rooms.RemoveRange(
                await context.Rooms.Where(r => r.Id == RoomId).ToListAsync());
            await context.SaveChangesAsync();

            context.RoomTypes.RemoveRange(
                await context.RoomTypes.Where(t => t.Id == RoomTypeId).ToListAsync());
            await context.SaveChangesAsync();
        }
    }
}
