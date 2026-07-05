using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
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
        }
    }
}
