using Microsoft.EntityFrameworkCore;
using BusinessObjects.Entities;
using Microsoft.Extensions.Configuration;

namespace DataAccessObjects
{
    public class HotelDbContext : DbContext
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Stay> Stays { get; set; }
        public DbSet<ServiceCategory> ServiceCategories { get; set; }
        public DbSet<ServiceItem> ServiceItems { get; set; }
        public DbSet<ServiceOrder> ServiceOrders { get; set; }
        public DbSet<ServiceOrderDetail> ServiceOrderDetails { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<SurchargeItem> SurchargeItems { get; set; }
        public DbSet<Surcharge> Surcharges { get; set; }
        public DbSet<GuestAccount> GuestAccounts { get; set; }
        public DbSet<HousekeepingRequest> HousekeepingRequests { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
            {
                return;
            }

            var configurationDirectory = ResolveConfigurationDirectory();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(configurationDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
                .Build();

            var connectionString = configuration.GetConnectionString("FUHotelManagement");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Thiếu ConnectionStrings:FUHotelManagement trong appsettings.json hoặc appsettings.Local.json.");
            }

            optionsBuilder.UseSqlServer(connectionString);
        }

        private static string ResolveConfigurationDirectory()
        {
            var candidates = new List<string>();
            AddDirectoryAndAncestors(candidates, AppContext.BaseDirectory);
            AddDirectoryAndAncestors(candidates, Directory.GetCurrentDirectory());

            var directory = candidates.Distinct(StringComparer.OrdinalIgnoreCase)
                .SelectMany(path => new[] { path, Path.Combine(path, "FUHotelManagementWPF") })
                .FirstOrDefault(path => File.Exists(Path.Combine(path, "appsettings.json")));

            return directory ?? throw new FileNotFoundException(
                "Không tìm thấy appsettings.json. Hãy chạy ứng dụng từ thư mục output hoặc chạy lệnh EF tại thư mục solution.");
        }

        private static void AddDirectoryAndAncestors(ICollection<string> paths, string startPath)
        {
            for (var directory = new DirectoryInfo(startPath);
                 directory != null;
                 directory = directory.Parent)
            {
                paths.Add(directory.FullName);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(HotelDbContext).Assembly);
        }
    }
}
