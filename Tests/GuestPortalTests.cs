using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

/// <summary>
/// Khach tu dang nhap (bang so dien thoai) va chi thay du lieu CUA CHINH MINH.
/// Kiem ca chieu thuan (dang nhap duoc, thay don cua minh) lan chieu chan
/// (sai mat khau, khong thay don nguoi khac, khong lam duoc viec cua nhan vien).
/// </summary>
[Collection("Db")]
public class GuestPortalTests
{
    private const string Password = "Khach@2026";

    [DbFact]
    public async Task Khach_DangNhapDuoc_VaChiThayDonCuaMinh()
    {
        await using var box = await Box.CreateAsync();
        try
        {
            // Le tan tao 2 khach, moi khach 1 don, roi kich hoat tai khoan cho khach A
            await TestUsers.SignInAsync("Receptionist");
            var (guestA, _) = await box.CreateGuestWithBookingAsync();
            var (guestB, _) = await box.CreateGuestWithBookingAsync();

            var activate = await new GuestAccountService().ActivateAsync(guestA.Id, Password);
            Assert.True(activate.Ok, activate.Message);
            AppSession.SignOut();

            // Khach A tu dang nhap
            var login = await new GuestAccountService().LoginAsync(guestA.PhoneNumber, Password);
            Assert.True(login.Ok, login.Message);
            AppSession.SignInGuest(login.Data!);
            Assert.True(AppSession.IsGuestLoggedIn);
            Assert.Equal(guestA.Id, AppSession.CurrentGuestId);
            // Khach KHONG co vai tro nhan vien -> moi service kiem quyen se tu chan
            Assert.Equal(string.Empty, AppSession.RoleName);

            // Chi thay don cua chinh minh, khong thay don cua khach B
            var mine = await new ReservationService().GetMyReservationsAsync();
            Assert.True(mine.Ok, mine.Message);
            Assert.Single(mine.Data!);
            Assert.Equal(guestA.Id, mine.Data![0].GuestId);
            Assert.DoesNotContain(mine.Data!, r => r.GuestId == guestB.Id);
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task Khach_SaiMatKhau_BiChan()
    {
        await using var box = await Box.CreateAsync();
        try
        {
            await TestUsers.SignInAsync("Receptionist");
            var (guest, _) = await box.CreateGuestWithBookingAsync();
            var activate = await new GuestAccountService().ActivateAsync(guest.Id, Password);
            Assert.True(activate.Ok, activate.Message);
            AppSession.SignOut();

            var sai = await new GuestAccountService().LoginAsync(guest.PhoneNumber, "SaiRoi@123");
            Assert.False(sai.Ok);
            Assert.False(string.IsNullOrWhiteSpace(sai.Message));
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task Khach_KhongLamDuocViecCuaNhanVien()
    {
        await using var box = await Box.CreateAsync();
        try
        {
            await TestUsers.SignInAsync("Receptionist");
            var (guest, reservation) = await box.CreateGuestWithBookingAsync();
            var activate = await new GuestAccountService().ActivateAsync(guest.Id, Password);
            Assert.True(activate.Ok, activate.Message);
            AppSession.SignOut();

            var login = await new GuestAccountService().LoginAsync(guest.PhoneNumber, Password);
            Assert.True(login.Ok, login.Message);
            AppSession.SignInGuest(login.Data!);

            // Dang phien khach: cac thao tac cua nhan vien deu phai bi chan
            var xacNhan = await new ReservationService().ConfirmAsync(reservation.Id);
            Assert.False(xacNhan.Ok);

            var taoKhach = await new GuestService().CreateAsync(
                "Khach gia mao", null, $"0{Random.Shared.NextInt64(900_000_000, 999_999_999)}", null, GuestTag.None, null);
            Assert.False(taoKhach.Ok);

            var taoUser = await new UserManagementService()
                .CreateAsync("Hacker", $"h{Guid.NewGuid():N}"[..10] + "@test.local", "Manh@2026Abc", 1);
            Assert.False(taoUser.Ok);
        }
        finally { AppSession.SignOut(); }
    }

    [DbFact]
    public async Task Khach_DoiMatKhau_RoiDangNhapBangMatKhauMoi()
    {
        await using var box = await Box.CreateAsync();
        try
        {
            await TestUsers.SignInAsync("Receptionist");
            var (guest, _) = await box.CreateGuestWithBookingAsync();
            Assert.True((await new GuestAccountService().ActivateAsync(guest.Id, Password)).Ok);
            AppSession.SignOut();

            const string moi = "MatKhauMoi@2026";
            var doi = await new GuestAccountService().ChangePasswordAsync(guest.Id, Password, moi);
            Assert.True(doi.Ok, doi.Message);

            // Mat khau cu khong dung duoc nua, mat khau moi thi vao duoc
            Assert.False((await new GuestAccountService().LoginAsync(guest.PhoneNumber, Password)).Ok);
            Assert.True((await new GuestAccountService().LoginAsync(guest.PhoneNumber, moi)).Ok);

            // Mat khau moi qua yeu thi bi chan
            var yeu = await new GuestAccountService().ChangePasswordAsync(guest.Id, moi, "123");
            Assert.False(yeu.Ok);
        }
        finally { AppSession.SignOut(); }
    }

    // ---------------------------------------------------------------- Tien ich

    private sealed class Box : IAsyncDisposable
    {
        public int RoomTypeId { get; private init; }
        public int RoomId { get; private init; }
        private readonly List<int> _guestIds = [];
        private int _dayOffset = 30; // moi don mot khoang ngay khac nhau, tranh trung lich

        public static async Task<Box> CreateAsync()
        {
            await using var db = HotelDbContextFactory.Create();
            var type = new RoomType
            {
                TypeName = $"GP{Guid.NewGuid():N}"[..14],
                Capacity = 2,
                BasePrice = 500_000m,
                IsActive = true,
            };
            db.RoomTypes.Add(type);
            await db.SaveChangesAsync();

            var room = new Room
            {
                RoomNumber = $"G{Guid.NewGuid():N}"[..9],
                Floor = 97,
                RoomTypeId = type.Id,
                Status = RoomStatus.Available,
                IsActive = true,
            };
            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            return new Box { RoomTypeId = type.Id, RoomId = room.Id };
        }

        public async Task<(Guest Guest, Reservation Reservation)> CreateGuestWithBookingAsync()
        {
            var suffix = $"gp{Guid.NewGuid():N}"[..12];
            var guest = await new GuestService().CreateAsync(
                $"Khach {suffix}", $"{suffix}@test.local", $"0{Random.Shared.NextInt64(900_000_000, 999_999_999)}",
                $"{Random.Shared.NextInt64(100_000_000_000, 999_999_999_999)}", GuestTag.None, null);
            Assert.True(guest.Ok, guest.Message);
            _guestIds.Add(guest.Data!.Id);

            var from = DateTime.Today.AddDays(_dayOffset);
            _dayOffset += 5;
            var booking = await new ReservationService().CreateAsync(
                guest.Data.Id, RoomId, 1, from, from.AddDays(1), null, null, null);
            Assert.True(booking.Ok, booking.Message);
            return (guest.Data, booking.Data!);
        }

        public async ValueTask DisposeAsync()
        {
            var guestIds = _guestIds;
            var roomId = RoomId;
            var typeId = RoomTypeId;

            await using var db = HotelDbContextFactory.Create();
            await db.GuestAccounts.Where(a => guestIds.Contains(a.GuestId)).ExecuteDeleteAsync();
            await db.Reservations.Where(r => guestIds.Contains(r.GuestId)).ExecuteDeleteAsync();
            await db.Guests.Where(g => guestIds.Contains(g.Id)).ExecuteDeleteAsync();
            await db.Rooms.Where(r => r.Id == roomId).ExecuteDeleteAsync();
            await db.RoomTypes.Where(t => t.Id == typeId).ExecuteDeleteAsync();
        }
    }
}
