using Microsoft.EntityFrameworkCore.Design;

namespace DataAccessObjects
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HotelDbContext>
    {
        public HotelDbContext CreateDbContext(string[] args)
        {
            return HotelDbContextFactory.Create();
        }
    }
}
