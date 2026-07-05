using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.BookingCode).IsRequired().HasMaxLength(50);
            builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(50);

            // Unique BookingCode
            builder.HasIndex(r => r.BookingCode).IsUnique();

            // Relationships
            builder.HasOne(r => r.Guest)
                   .WithMany(g => g.Reservations)
                   .HasForeignKey(r => r.GuestId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Room)
                   .WithMany(rm => rm.Reservations)
                   .HasForeignKey(r => r.RoomId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Check Constraint: CheckOutDate > CheckInDate
            builder.ToTable(t => t.HasCheckConstraint("CK_Reservation_CheckOutDate_CheckInDate", "[CheckOutDate] > [CheckInDate]"));
        }
    }
}
