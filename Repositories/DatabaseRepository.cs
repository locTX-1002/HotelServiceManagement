using DataAccessObjects;

namespace Repositories;

public sealed class DatabaseRepository
{
    public Task EnsureMigratedAsync() => HotelDbContextFactory.EnsureMigratedAsync();

    /// <summary>Du lieu mau cho database moi tinh - da co du lieu roi thi khong dung vao.</summary>
    public Task SeedDemoDataAsync() => DemoDataDAO.SeedAsync();
}
