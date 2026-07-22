using DataAccessObjects;

namespace Repositories;

public sealed class DatabaseRepository
{
    public Task EnsureMigratedAsync() => HotelDbContextFactory.EnsureMigratedAsync();
}
