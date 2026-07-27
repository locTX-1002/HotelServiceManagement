using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

/// <summary>
/// Luong tu phuc vu moi cua khach: tu dat phong va gui yeu cau huy don cua chinh minh.
/// Cac test dung DB that de bat loi FK/transaction va loi ownership.
/// </summary>
[Collection("Db")]
public sealed class GuestSelfServiceTests : IAsyncLifetime
{
    private const string Password = "Khach@2026";
    private readonly List<int> _guestIds = [];
    private readonly List<int> _roomIds = [];
    private readonly List<int> _roomTypeIds = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        AppSession.SignOut();
        await using var db = HotelDbContextFactory.Create();

        var reservationIds = await db.Reservations
            .Where(x => _guestIds.Contains(x.GuestId))
            .Select(x => x.Id)
            .ToListAsync();

        if (reservationIds.Count > 0)
        {
            await db.ApprovalRequests
                .Where(x => x.RequestType == ApprovalRequestType.ReservationCancel
                            && reservationIds.Contains(x.TargetId))
                .ExecuteDeleteAsync();
            await db.Reservations.Where(x => reservationIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        if (_guestIds.Count > 0)
        {
            await db.GuestAccounts.Where(x => _guestIds.Contains(x.GuestId)).ExecuteDeleteAsync();
            await db.Guests.Where(x => _guestIds.Contains(x.Id)).ExecuteDeleteAsync();
        }
        if (_roomIds.Count > 0)
            await db.Rooms.Where(x => _roomIds.Contains(x.Id)).ExecuteDeleteAsync();
        if (_roomTypeIds.Count > 0)
            await db.RoomTypes.Where(x => _roomTypeIds.Contains(x.Id)).ExecuteDeleteAsync();
    }

    [DbFact]
    public async Task Khach_TuDatPhong_DonThuocDungGuest_VaOTrangThaiChoXacNhan()
    {
        var guest = await CreateGuestAccountAndSignInAsync();
        var room = await CreateRoomAsync(capacity: 2);
        var checkIn = DateTime.Today.AddDays(20);
        var checkOut = checkIn.AddDays(2);

        var result = await new ReservationService().CreateForCurrentGuestAsync(
            room.Id, 2, checkIn, checkOut, "Phong yen tinh");

        Assert.True(result.Ok, result.Message);
        Assert.Equal(guest.Id, result.Data!.GuestId);
        Assert.Null(result.Data.CreatedByUserId);
        Assert.Equal(ReservationStatus.Pending, result.Data.Status);
        Assert.Null(result.Data.DepositAmount);

        var mine = await new ReservationService().GetMyReservationsAsync();
        Assert.True(mine.Ok, mine.Message);
        Assert.Contains(mine.Data!, x => x.Id == result.Data.Id && x.GuestId == guest.Id);
    }

    [DbFact]
    public async Task Khach_GuiYeuCauHuy_DonVanPending_DenKhiManagerDuyet()
    {
        var guest = await CreateGuestAccountAndSignInAsync();
        var room = await CreateRoomAsync(capacity: 2);
        var checkIn = DateTime.Today.AddDays(30);

        var booking = await new ReservationService().CreateForCurrentGuestAsync(
            room.Id, 1, checkIn, checkIn.AddDays(2), null);
        Assert.True(booking.Ok, booking.Message);

        var request = await new ApprovalService().RequestGuestReservationCancellationAsync(
            booking.Data!.Id, "Thay đổi kế hoạch chuyến đi");

        Assert.True(request.Ok, request.Message);
        Assert.Equal(guest.Id, request.Data!.RequestedByGuestId);
        Assert.Null(request.Data.RequestedByUserId);
        Assert.Equal(ApprovalRequestStatus.Pending, request.Data.Status);

        await using (var db = HotelDbContextFactory.Create())
        {
            var reservation = await db.Reservations.AsNoTracking()
                .SingleAsync(x => x.Id == booking.Data.Id);
            Assert.Equal(ReservationStatus.Pending, reservation.Status);
        }

        // Gui lap lan hai phai bi chan, tranh hai request Pending cho cung mot don.
        var duplicate = await new ApprovalService().RequestGuestReservationCancellationAsync(
            booking.Data.Id, "Gui lai");
        Assert.False(duplicate.Ok);

        AppSession.SignOut();
        await TestUsers.SignInAsync(RoleNames.Manager);
        var reviewed = await new ApprovalService().ReviewAsync(request.Data.Id, true, null);
        Assert.True(reviewed.Ok, reviewed.Message);

        await using var verify = HotelDbContextFactory.Create();
        var cancelled = await verify.Reservations.AsNoTracking()
            .SingleAsync(x => x.Id == booking.Data.Id);
        Assert.Equal(ReservationStatus.Cancelled, cancelled.Status);
    }

    [DbFact]
    public async Task Khach_KhongTheGuiYeuCauHuy_DonCuaKhachKhac()
    {
        var guestA = await CreateGuestAccountAndSignInAsync();
        var room = await CreateRoomAsync(capacity: 2);
        var checkIn = DateTime.Today.AddDays(40);

        // Tao guest B va booking bang Receptionist, sau do dang nhap lai guest A.
        AppSession.SignOut();
        await TestUsers.SignInAsync(RoleNames.Receptionist);
        var guestBResult = await new GuestService().CreateAsync(
            $"Khach B {Guid.NewGuid():N}"[..20],
            $"b{Guid.NewGuid():N}"[..12] + "@test.local",
            $"0{Random.Shared.NextInt64(900_000_000, 999_999_999)}",
            null, GuestTag.None, null);
        Assert.True(guestBResult.Ok, guestBResult.Message);
        var guestB = guestBResult.Data!;
        _guestIds.Add(guestB.Id);

        var otherBooking = await new ReservationService().CreateAsync(
            guestB.Id, room.Id, 1, checkIn, checkIn.AddDays(2), null, null, null);
        Assert.True(otherBooking.Ok, otherBooking.Message);

        AppSession.SignOut();
        var loginA = await new GuestAccountService().LoginAsync(guestA.PhoneNumber, Password);
        Assert.True(loginA.Ok, loginA.Message);
        AppSession.SignInGuest(loginA.Data!);

        var result = await new ApprovalService().RequestGuestReservationCancellationAsync(
            otherBooking.Data!.Id, "Thu huy don nguoi khac");

        Assert.False(result.Ok);
        Assert.Contains("của bạn", result.Message);
    }

    private async Task<Guest> CreateGuestAccountAndSignInAsync()
    {
        AppSession.SignOut();
        await TestUsers.SignInAsync(RoleNames.Receptionist);

        var suffix = Guid.NewGuid().ToString("N")[..10];
        var created = await new GuestService().CreateAsync(
            $"Guest {suffix}",
            $"{suffix}@test.local",
            $"0{Random.Shared.NextInt64(900_000_000, 999_999_999)}",
            null,
            GuestTag.None,
            null);
        Assert.True(created.Ok, created.Message);
        _guestIds.Add(created.Data!.Id);

        var activated = await new GuestAccountService().ActivateAsync(created.Data.Id, Password);
        Assert.True(activated.Ok, activated.Message);

        AppSession.SignOut();
        var login = await new GuestAccountService().LoginAsync(created.Data.PhoneNumber, Password);
        Assert.True(login.Ok, login.Message);
        AppSession.SignInGuest(login.Data!);
        return created.Data;
    }

    private async Task<Room> CreateRoomAsync(int capacity)
    {
        await using var db = HotelDbContextFactory.Create();
        var type = new RoomType
        {
            TypeName = $"Online-{Guid.NewGuid():N}"[..18],
            Capacity = capacity,
            BasePrice = 650_000m,
            IsActive = true
        };
        db.RoomTypes.Add(type);
        await db.SaveChangesAsync();
        _roomTypeIds.Add(type.Id);

        var room = new Room
        {
            RoomNumber = $"O{Guid.NewGuid():N}"[..8],
            Floor = 96,
            RoomTypeId = type.Id,
            RoomType = type,
            Status = RoomStatus.Available,
            IsActive = true
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        _roomIds.Add(room.Id);
        return room;
    }
}
