using BusinessObjects.Entities;
using BusinessObjects.Enums;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Services;

namespace HotelManagement.Tests;

/// <summary>Guest dang o tu goi dich vu cho chinh phong cua minh.</summary>
[Collection("Db")]
public sealed class GuestServiceOrderTests : IAsyncLifetime
{
    private const string Password = "GuestService@2026";
    private readonly List<int> _guestIds = [];
    private readonly List<int> _reservationIds = [];
    private readonly List<int> _stayIds = [];
    private readonly List<int> _roomIds = [];
    private readonly List<int> _roomTypeIds = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        AppSession.SignOut();
        await using var db = HotelDbContextFactory.Create();

        var orderIds = _stayIds.Count == 0
            ? []
            : await db.ServiceOrders.Where(x => _stayIds.Contains(x.StayId)).Select(x => x.Id).ToListAsync();

        if (orderIds.Count > 0)
        {
            await db.ServiceOrderDetails.Where(x => orderIds.Contains(x.ServiceOrderId)).ExecuteDeleteAsync();
            await db.ServiceOrders.Where(x => orderIds.Contains(x.Id)).ExecuteDeleteAsync();
        }
        if (_stayIds.Count > 0)
            await db.Stays.Where(x => _stayIds.Contains(x.Id)).ExecuteDeleteAsync();
        if (_reservationIds.Count > 0)
            await db.Reservations.Where(x => _reservationIds.Contains(x.Id)).ExecuteDeleteAsync();
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
    public async Task KhachDangO_TuGoiDichVu_DonGanDungStay_VaKhongGiaNhanVien()
    {
        var account = await RegisterAndSignInAsync();
        var stay = await CreateActiveStayAsync(account.GuestId);
        var item = await GetAvailableItemAsync();

        var result = await new GuestServiceOrderService().CreateAsync(
            [new ServiceOrderLine(item.Id, 2)]);

        Assert.True(result.Ok, result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(stay.Id, result.Data!.StayId);
        Assert.Null(result.Data.CreatedByUserId);
        Assert.Equal(ServiceOrderStatus.Pending, result.Data.Status);
        Assert.Equal(item.UnitPrice * 2, result.Data.TotalAmount);

        await using var db = HotelDbContextFactory.Create();
        var saved = await db.ServiceOrders.AsNoTracking()
            .Include(x => x.OrderDetails)
            .SingleAsync(x => x.Id == result.Data.Id);
        Assert.Equal(stay.Id, saved.StayId);
        Assert.Single(saved.OrderDetails);
        Assert.Equal(2, saved.OrderDetails.Single().Quantity);
        Assert.Equal(item.UnitPrice, saved.OrderDetails.Single().UnitPrice);
    }

    [DbFact]
    public async Task KhachChuaNhanPhong_KhongTheGoiDichVu()
    {
        await RegisterAndSignInAsync();
        var item = await GetAvailableItemAsync();

        var result = await new GuestServiceOrderService().CreateAsync(
            [new ServiceOrderLine(item.Id, 1)]);

        Assert.False(result.Ok);
        Assert.Contains("nhận phòng", result.Message);
    }

    [DbFact]
    public async Task GuestA_ResolveStayChiTraVePhongCuaChinhGuestA()
    {
        var guestA = await RegisterAndSignInAsync();
        var stayA = await CreateActiveStayAsync(guestA.GuestId);

        AppSession.SignOut();
        var guestB = await RegisterAndSignInAsync();
        var stayB = await CreateActiveStayAsync(guestB.GuestId);

        AppSession.SignOut();
        var loginA = await new GuestAccountService().LoginAsync(guestA.Guest.PhoneNumber, Password);
        Assert.True(loginA.Ok, loginA.Message);
        AppSession.SignInGuest(loginA.Data!);

        var result = await new GuestServiceOrderService().GetCurrentActiveStayAsync();

        Assert.True(result.Ok, result.Message);
        Assert.Equal(stayA.Id, result.Data!.Id);
        Assert.NotEqual(stayB.Id, result.Data.Id);
    }

    private async Task<GuestAccount> RegisterAndSignInAsync()
    {
        AppSession.SignOut();
        var phone = $"0{Random.Shared.NextInt64(900_000_000, 999_999_999)}";
        var result = await new GuestAccountService().RegisterAsync(
            $"Guest Service {Guid.NewGuid():N}"[..25],
            phone,
            null,
            Password,
            Password);
        Assert.True(result.Ok, result.Message);
        _guestIds.Add(result.Data!.GuestId);
        AppSession.SignInGuest(result.Data);
        return result.Data;
    }

    private async Task<Stay> CreateActiveStayAsync(int guestId)
    {
        await using var db = HotelDbContextFactory.Create();

        var roomType = new RoomType
        {
            TypeName = $"GuestSvc-{Guid.NewGuid():N}"[..22],
            Capacity = 2,
            BasePrice = 700_000m,
            IsActive = true
        };
        db.RoomTypes.Add(roomType);
        await db.SaveChangesAsync();
        _roomTypeIds.Add(roomType.Id);

        var room = new Room
        {
            RoomNumber = $"S{Guid.NewGuid():N}"[..8],
            Floor = 95,
            RoomTypeId = roomType.Id,
            Status = RoomStatus.Occupied,
            IsActive = true
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        _roomIds.Add(room.Id);

        var reservation = new Reservation
        {
            BookingCode = $"GS-{Guid.NewGuid():N}"[..20],
            GuestId = guestId,
            RoomId = room.Id,
            NumberOfGuests = 1,
            CheckInDate = DateTime.Today.AddDays(-1),
            CheckOutDate = DateTime.Today.AddDays(2),
            Status = ReservationStatus.CheckedIn
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();
        _reservationIds.Add(reservation.Id);

        var stay = new Stay
        {
            ReservationId = reservation.Id,
            ActualCheckIn = DateTime.Now.AddHours(-6),
            Status = StayStatus.Active
        };
        db.Stays.Add(stay);
        await db.SaveChangesAsync();
        _stayIds.Add(stay.Id);
        return stay;
    }

    private static async Task<ServiceItem> GetAvailableItemAsync()
    {
        await using var db = HotelDbContextFactory.Create();
        return await db.ServiceItems.AsNoTracking()
            .Include(x => x.ServiceCategory)
            .FirstAsync(x => x.IsAvailable && x.ServiceCategory.IsActive);
    }
}
