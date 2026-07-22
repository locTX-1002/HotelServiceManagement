using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccessObjects
{
    /// <summary>
    /// Diem tao DbContext DUY NHAT luc runtime - moi DAO goi qua day de dam bao ca app dung chung
    /// 1 connection string doc tu appsettings.json (nam canh file .exe). Khong hardcode chuoi ket
    /// noi rai rac trong tung DAO.
    /// </summary>
    public static class HotelDbContextFactory
    {
        private const string FallbackConnection =
            "Server=.\\SQLEXPRESS;Database=FUHotelManagementDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

        private static string? _connectionString;

        private static string ConnectionString
        {
            get
            {
                if (_connectionString == null)
                {
                    var config = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: true)
                        // File override rieng tung may (da gitignore) - ai dung instance khac
                        // SQLEXPRESS thi tao appsettings.Local.json de khoi dung cham file chung.
                        .AddJsonFile("appsettings.Local.json", optional: true)
                        .Build();
                    // Thieu appsettings van chay duoc bang mac dinh SQLEXPRESS - do la quy uoc chung
                    // ca nhom dang dung; may nao instance khac thi chi can sua appsettings.json.
                    _connectionString = config.GetConnectionString("FUHotelManagement") ?? FallbackConnection;
                }
                return _connectionString;
            }
        }

        public static HotelDbContext Create()
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;
            return new HotelDbContext(options);
        }

        /// <summary>
        /// Ap moi migration con thieu vao database (tu tao DB neu chua co) - goi 1 lan luc app
        /// khoi dong, giong co che cua ban web: thanh vien moi chi can F5 la DB tu dung.
        /// </summary>
        public static async Task EnsureMigratedAsync()
        {
            await using var context = Create();
            await context.Database.MigrateAsync();
        }
    }
}
