using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Services;

namespace HotelManagement.Tests;

public class AuthorizationBoundaryTests
{
    [Fact]
    public async Task ServiceStaff_CannotForgeRoomMaintenancePermission()
    {
        SignInAs("ServiceStaff");

        var result = await new RoomService()
            .UpdateStatusAsync(1, RoomStatus.Maintenance, canManageMaintenance: true);

        Assert.False(result.Ok);
        Assert.Contains("Admin", result.Message);
    }

    [Fact]
    public async Task Receptionist_CannotCreateRoomType()
    {
        SignInAs("Receptionist");

        var result = await new RoomTypeService().CreateAsync("VIP", 2, 1_000_000, null, true);

        Assert.False(result.Ok);
    }

    [Fact]
    public async Task ServiceStaff_CannotCreateGuest()
    {
        SignInAs("ServiceStaff");

        var result = await new GuestService().CreateAsync(
            "Guest", null, "0900000000", null, GuestTag.None, null);

        Assert.False(result.Ok);
    }

    [Fact]
    public async Task ServiceStaff_CannotCreateReservation()
    {
        SignInAs("ServiceStaff");

        var result = await new ReservationService().CreateAsync(
            1, 1, 1, DateTime.Today, DateTime.Today.AddDays(1), null, null, null);

        Assert.False(result.Ok);
    }

    [Fact]
    public async Task Receptionist_CannotManageServiceCatalog()
    {
        SignInAs("Receptionist");

        var result = await new ServiceCatalogService().SaveCategoryAsync(null, "Spa", true);

        Assert.False(result.Ok);
    }

    [Fact]
    public async Task ServiceStaff_CannotActivateGuestAccount()
    {
        SignInAs("ServiceStaff");

        var result = await new GuestAccountService().ActivateAsync(1, "Strong@2026");

        Assert.False(result.Ok);
    }

    private static void SignInAs(string roleName) => AppSession.SignIn(new User
    {
        Id = 99,
        Role = new Role { RoleName = roleName },
    });
}
