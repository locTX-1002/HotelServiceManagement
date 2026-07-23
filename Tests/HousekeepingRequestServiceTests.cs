using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;
using Services;

namespace HotelManagement.Tests;

public class HousekeepingRequestServiceTests
{
    [Fact]
    public async Task CompletedRequest_CannotReturnToPending()
    {
        AppSession.SignIn(new User { Id = 2, Role = new Role { RoleName = "ServiceStaff" } });
        var request = new HousekeepingRequest { Id = 1, Status = HousekeepingRequestStatus.Completed };
        var repository = new FakeRepository(request);

        var result = await new HousekeepingRequestService(repository)
            .ChangeStatusAsync(1, HousekeepingRequestStatus.Pending);

        Assert.False(result.Ok);
        Assert.False(repository.SaveWasCalled);
    }

    private sealed class FakeRepository(HousekeepingRequest request) : IHousekeepingRequestRepository
    {
        public bool SaveWasCalled { get; private set; }
        public Task<List<HousekeepingRequest>> GetAllAsync() => Task.FromResult(new List<HousekeepingRequest>());
        public Task<HousekeepingRequest?> GetByIdAsync(int id) => Task.FromResult<HousekeepingRequest?>(request);
        public Task<bool> IsStayActiveAsync(int stayId) => Task.FromResult(true);
        public Task SaveAsync(HousekeepingRequest entity, bool add) { SaveWasCalled = true; return Task.CompletedTask; }
    }
}
