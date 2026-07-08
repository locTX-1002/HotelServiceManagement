using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.RoomCharge).HasPrecision(18, 2);
            builder.Property(i => i.ServiceCharge).HasPrecision(18, 2);
            builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
            builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(50);

            // One stay can generate zero or one invoice.
            builder.HasOne(i => i.Stay)
                   .WithOne(s => s.Invoice)
                   .HasForeignKey<Invoice>(i => i.StayId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.CreatedByUser)
                   .WithMany()
                   .HasForeignKey(i => i.CreatedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
