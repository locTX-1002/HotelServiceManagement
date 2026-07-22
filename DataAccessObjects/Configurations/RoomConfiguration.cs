using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace DataAccessObjects.Configurations
{
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.RoomNumber).IsRequired().HasMaxLength(20);
            builder.Property(r => r.Floor).IsRequired();
            builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(r => r.IsActive).HasDefaultValue(true);

            // Unique RoomNumber
            builder.HasIndex(r => r.RoomNumber).IsUnique();

            // Relationship
            builder.HasOne(r => r.RoomType)
                   .WithMany(rt => rt.Rooms)
                   .HasForeignKey(r => r.RoomTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Seed some default Rooms with Floor and IsActive
            builder.HasData(
                new Room { Id = 1, RoomNumber = "101", Floor = 1, RoomTypeId = 1, Status = RoomStatus.Available, IsActive = true },
                new Room { Id = 2, RoomNumber = "102", Floor = 1, RoomTypeId = 1, Status = RoomStatus.Available, IsActive = true },
                new Room { Id = 3, RoomNumber = "201", Floor = 2, RoomTypeId = 2, Status = RoomStatus.Available, IsActive = true },
                new Room { Id = 4, RoomNumber = "202", Floor = 2, RoomTypeId = 2, Status = RoomStatus.Available, IsActive = true },
                new Room { Id = 5, RoomNumber = "301", Floor = 3, RoomTypeId = 3, Status = RoomStatus.Available, IsActive = true },
                new Room { Id = 6, RoomNumber = "401", Floor = 4, RoomTypeId = 4, Status = RoomStatus.Available, IsActive = true }
            );
        }
    }
}
