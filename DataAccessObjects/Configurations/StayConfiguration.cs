using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessObjects.Entities;

namespace DataAccessObjects.Configurations
{
    public class StayConfiguration : IEntityTypeConfiguration<Stay>
    {
        public void Configure(EntityTypeBuilder<Stay> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(50);

            // One reservation can create zero or one stay.
            builder.HasOne(s => s.Reservation)
                   .WithOne(r => r.Stay)
                   .HasForeignKey<Stay>(s => s.ReservationId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.CheckedInByUser)
                   .WithMany()
                   .HasForeignKey(s => s.CheckedInByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.CheckedOutByUser)
                   .WithMany()
                   .HasForeignKey(s => s.CheckedOutByUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
