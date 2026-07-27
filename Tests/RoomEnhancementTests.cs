using BusinessObjects;
using Services;

namespace HotelManagement.Tests;

[Collection("Db")]
public class RoomEnhancementTests
{
    private readonly IReservationService _reservationService = new ReservationService();

    [DbFact]
    public async Task GetRecentByRoomAsync_ReturnsReservationsOrderedByCheckInDate()
    {
        // Arrange
        var user = await TestUsers.GetAsync("Manager");
        AppSession.SignIn(user);

        // Act
        var result = await _reservationService.GetRecentByRoomAsync(1, 5);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count <= 5);
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].CheckInDate >= result[i + 1].CheckInDate);
        }
    }
}
