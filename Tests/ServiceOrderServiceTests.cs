using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;
using Services;

namespace HotelManagement.Tests;

public class ServiceOrderServiceTests
{
    [Fact]
    public async Task CompletedOrder_CannotChangeStatus()
    {
        var repository = new FakeOrderRepository(new ServiceOrder { Id = 1, Status = ServiceOrderStatus.Completed });
        var service = new ServiceOrderService(repository, new FakeCatalogRepository());

        var result = await service.ChangeStatusAsync(1, ServiceOrderStatus.Cancelled);

        Assert.False(result.Ok);
        Assert.False(repository.ChangeWasCalled);
    }

    [Fact]
    public async Task PendingOrder_CanMoveToProcessing()
    {
        var repository = new FakeOrderRepository(new ServiceOrder { Id = 1, Status = ServiceOrderStatus.Pending });
        var service = new ServiceOrderService(repository, new FakeCatalogRepository());

        var result = await service.ChangeStatusAsync(1, ServiceOrderStatus.Processing);

        Assert.True(result.Ok);
        Assert.Equal(ServiceOrderStatus.Processing, result.Data!.Status);
    }

    private sealed class FakeOrderRepository(ServiceOrder order) : IServiceOrderRepository
    {
        public bool ChangeWasCalled { get; private set; }
        public Task<List<ServiceOrder>> GetByStayAsync(int id) => Task.FromResult(new List<ServiceOrder> { order });
        public Task<ServiceOrder?> GetByIdAsync(int id) => Task.FromResult<ServiceOrder?>(order);
        public Task<bool> IsStayActiveAsync(int id) => Task.FromResult(true);
        public Task AddAsync(ServiceOrder value) => Task.CompletedTask;
        public Task<ServiceOrder?> ChangeStatusAsync(int id, ServiceOrderStatus status) { ChangeWasCalled = true; order.Status = status; return Task.FromResult<ServiceOrder?>(order); }
    }

    private sealed class FakeCatalogRepository : IServiceCatalogRepository
    {
        public Task<List<ServiceCategory>> GetCategoriesAsync() => Task.FromResult(new List<ServiceCategory>());
        public Task<List<ServiceItem>> GetItemsAsync(bool availableOnly = false) => Task.FromResult(new List<ServiceItem>());
        public Task<ServiceCategory?> GetCategoryAsync(int id) => Task.FromResult<ServiceCategory?>(null);
        public Task<ServiceItem?> GetItemAsync(int id) => Task.FromResult<ServiceItem?>(null);
        public Task SaveCategoryAsync(ServiceCategory value, bool add) => Task.CompletedTask;
        public Task SaveItemAsync(ServiceItem value, bool add) => Task.CompletedTask;
    }
}
