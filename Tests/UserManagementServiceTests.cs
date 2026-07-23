using BusinessObjects.Entities;
using Repositories;
using Services;

namespace HotelManagement.Tests;

public class UserManagementServiceTests
{
    [Fact]
    public async Task Admin_CannotDeactivateCurrentAccount()
    {
        var admin = new User { Id = 1, IsActive = true, Role = new Role { RoleName = "Admin" } };
        AppSession.SignIn(admin);
        var repository = new FakeUserRepository(admin);

        var result = await new UserManagementService(repository).SetActiveAsync(1, false);

        Assert.False(result.Ok);
        Assert.True(admin.IsActive);
        Assert.False(repository.SaveWasCalled);
    }

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public bool SaveWasCalled { get; private set; }
        public Task<User?> GetActiveByEmailAsync(string email) => Task.FromResult<User?>(user);
        public Task<List<User>> GetAllAsync() => Task.FromResult(new List<User> { user });
        public Task<User?> GetByIdAsync(int id) => Task.FromResult<User?>(user);
        public Task<Role?> GetRoleAsync(int id) => Task.FromResult<Role?>(user.Role);
        public Task<bool> EmailExistsAsync(string email, int? excludeId = null) => Task.FromResult(false);
        public Task SaveAsync(User entity, bool add) { SaveWasCalled = true; return Task.CompletedTask; }
        public Task EnsureBootstrapAdminAsync(string fullName, string email, string passwordHash) => Task.CompletedTask;
    }
}
