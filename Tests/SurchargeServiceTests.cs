using BusinessObjects.Entities;
using Repositories;
using Services;

namespace HotelManagement.Tests;

public class SurchargeServiceTests
{
    [Fact]
    public async Task CannotDeleteSurchargeAfterPayment()
    {
        AppSession.SignIn(new User { Role = new Role { RoleName = "Manager" } });
        var repository = new FakeRepository { DeleteSucceeds = false };

        var result = await new SurchargeService(repository).DeleteAsync(1);

        Assert.False(result.Ok);
        Assert.True(repository.DeleteWasCalled);
    }

    private sealed class FakeRepository : ISurchargeRepository
    {
        public bool DeleteSucceeds { get; init; }
        public bool DeleteWasCalled { get; private set; }
        public Task<List<SurchargeItem>> GetItemsAsync() => Task.FromResult(new List<SurchargeItem>());
        public Task<SurchargeItem?> GetItemAsync(int id) => Task.FromResult<SurchargeItem?>(null);
        public Task SaveItemAsync(SurchargeItem value, bool add) => Task.CompletedTask;
        public Task<List<Surcharge>> GetByStayAsync(int id) => Task.FromResult(new List<Surcharge>());
        public Task<Surcharge?> AddToStayAsync(int stayId, int itemId, int quantity, int? userId) => Task.FromResult<Surcharge?>(null);
        public Task<Surcharge?> UpdateAsync(int id, int quantity) => Task.FromResult<Surcharge?>(null);
        public Task<bool> DeleteAsync(int id) { DeleteWasCalled = true; return Task.FromResult(DeleteSucceeds); }
    }
}
