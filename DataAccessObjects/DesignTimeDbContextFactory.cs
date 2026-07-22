using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataAccessObjects
{
    // Chi dung cho lenh `dotnet ef migrations ...` luc DEV (design-time) - runtime that doc
    // connection string tu appsettings.json cua project WPF. Server SQLEXPRESS + Trusted_Connection
    // la quy uoc chung cua nhom; may dung instance khac thi truyen cau hinh phu hop
    // tao migration (viec hiem, thuong chi 1 nguoi giu vai tro DB lam).
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HotelDbContext>
    {
        public HotelDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseSqlServer("Server=.\\SQLEXPRESS;Database=FUHotelManagementDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True")
                .Options;
            return new HotelDbContext(options);
        }
    }
}
