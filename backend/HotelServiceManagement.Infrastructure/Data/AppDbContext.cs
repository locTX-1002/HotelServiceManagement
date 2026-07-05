using HotelServiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Stay> Stays => Set<Stay>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<ServiceOrderDetail> ServiceOrderDetails => Set<ServiceOrderDetail>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Role>(e =>
        {
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleName).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.RoleName).IsUnique();
        });

        mb.Entity<User>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        });

        mb.Entity<RoomType>(e =>
        {
            e.HasKey(x => x.RoomTypeId);
            e.Property(x => x.TypeName).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.TypeName).IsUnique();
            e.Property(x => x.BasePrice).HasPrecision(18, 2);
        });

        mb.Entity<Room>(e =>
        {
            e.HasKey(x => x.RoomId);
            e.Property(x => x.RoomNumber).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.RoomNumber).IsUnique(); // BR01
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        mb.Entity<Guest>(e =>
        {
            e.HasKey(x => x.GuestId);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.IdentityNumber).HasMaxLength(20);
            e.Property(x => x.Address).HasMaxLength(255);
        });

        mb.Entity<Reservation>(e =>
        {
            e.HasKey(x => x.ReservationId);
            e.Property(x => x.BookingCode).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.BookingCode).IsUnique();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.RoomId, x.CheckInDate, x.CheckOutDate }); // hỗ trợ query BR03
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Reservations_DateRange", "[CheckOutDate] > [CheckInDate]")); // BR02
        });

        mb.Entity<Stay>(e =>
        {
            e.HasKey(x => x.StayId);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Reservation)
                .WithOne(r => r.Stay)
                .HasForeignKey<Stay>(x => x.ReservationId); // unique: 1 stay / 1 reservation
        });

        mb.Entity<ServiceCategory>(e =>
        {
            e.HasKey(x => x.ServiceCategoryId);
            e.Property(x => x.CategoryName).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.CategoryName).IsUnique();
        });

        mb.Entity<ServiceItem>(e =>
        {
            e.HasKey(x => x.ServiceItemId);
            e.Property(x => x.ServiceName).HasMaxLength(100).IsRequired();
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
        });

        mb.Entity<ServiceOrder>(e =>
        {
            e.HasKey(x => x.ServiceOrderId);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
        });

        mb.Entity<ServiceOrderDetail>(e =>
        {
            e.HasKey(x => x.DetailId);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.ToTable(t => t.HasCheckConstraint(
                "CK_ServiceOrderDetails_Quantity", "[Quantity] > 0"));
        });

        mb.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.InvoiceId);
            e.Property(x => x.RoomCharge).HasPrecision(18, 2);
            e.Property(x => x.ServiceCharge).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Stay)
                .WithOne(s => s.Invoice)
                .HasForeignKey<Invoice>(x => x.StayId); // unique: 1 invoice / 1 stay
        });

        mb.Entity<Payment>(e =>
        {
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Method).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.ToTable(t => t.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0"));
        });

        // Tránh multiple cascade paths trên SQL Server: mọi FK đều Restrict,
        // việc xóa nghiệp vụ dùng cờ IsActive / status thay vì xóa vật lý.
        foreach (var fk in mb.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
