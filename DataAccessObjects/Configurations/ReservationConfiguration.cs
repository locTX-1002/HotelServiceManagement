using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccessObjects.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.BookingCode)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(r => r.NumberOfGuests)
                   .IsRequired()
                   .HasDefaultValue(1);

            builder.Property(r => r.Status)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            builder.Property(r => r.SpecialRequests)
                   .HasMaxLength(500);

            builder.Property(r => r.DepositAmount)
                   .HasPrecision(18, 2);

            builder.Property(r => r.DepositPaymentMethod)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            builder.Property(r => r.RowVersion)
                   .IsRowVersion();

            builder.HasIndex(r => r.BookingCode).IsUnique();

            builder.HasOne(r => r.Guest)
                   .WithMany(g => g.Reservations)
                   .HasForeignKey(r => r.GuestId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Room)
                   .WithMany(rm => rm.Reservations)
                   .HasForeignKey(r => r.RoomId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.CreatedByUser)
                   .WithMany()
                   .HasForeignKey(r => r.CreatedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // DB can enforce rules that only depend on columns in Reservations.
            // Maximum capacity still belongs in ReservationService because Capacity is in RoomTypes.
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Reservation_CheckOutDate_CheckInDate",
                    "[CheckOutDate] > [CheckInDate]");

                t.HasCheckConstraint(
                    "CK_Reservation_NumberOfGuests_MinValue",
                    "[NumberOfGuests] >= 1");

                t.HasCheckConstraint(
                    "CK_Reservation_DepositAmount_NonNegative",
                    "[DepositAmount] IS NULL OR [DepositAmount] >= 0");
            });
        }
    }
}