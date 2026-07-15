using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        public void Configure(EntityTypeBuilder<Promotion> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Code)
                   .IsRequired()
                   .HasMaxLength(30);

            builder.Property(p => p.Description)
                   .HasMaxLength(255);

            builder.Property(p => p.Type)
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(p => p.Value)
                   .HasPrecision(18, 2);

            builder.Property(p => p.IsActive)
                   .HasDefaultValue(true);

            builder.HasIndex(p => p.Code).IsUnique();
        }
    }
}
