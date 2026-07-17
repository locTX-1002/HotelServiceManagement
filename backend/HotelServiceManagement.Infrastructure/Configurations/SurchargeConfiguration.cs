using HotelServiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelServiceManagement.Infrastructure.Configurations;

public class SurchargeConfiguration : IEntityTypeConfiguration<Surcharge>
{
    public void Configure(EntityTypeBuilder<Surcharge> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.UnitPriceSnapshot).HasPrecision(18, 2);
        builder.Property(s => s.Subtotal).HasPrecision(18, 2);
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasOne(s => s.Stay)
            .WithMany(s => s.Surcharges)
            .HasForeignKey(s => s.StayId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.SurchargeItem)
            .WithMany(i => i.Surcharges)
            .HasForeignKey(s => s.SurchargeItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
