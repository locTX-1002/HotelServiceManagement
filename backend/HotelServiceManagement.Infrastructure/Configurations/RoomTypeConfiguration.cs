using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
    {
        public void Configure(EntityTypeBuilder<RoomType> builder)
        {
            builder.HasKey(rt => rt.Id);
            
            builder.Property(rt => rt.TypeName)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(rt => rt.Capacity)
                   .IsRequired();

            builder.Property(rt => rt.BasePrice)
                   .HasPrecision(18, 2);

            builder.Property(rt => rt.IsActive)
                   .HasDefaultValue(true);

            // Unique Typename
            builder.HasIndex(rt => rt.TypeName).IsUnique();

            // Seed RoomTypes - giá thực tế VND (khớp TYPE_PRICES ở frontend/src/pages/HomePage.jsx),
            // trước đây dùng số nhỏ (100/180/300/250) chỉ để test cho gọn, không phải giá thật.
            builder.HasData(
                new RoomType { Id = 1, TypeName = "Standard", Capacity = 2, BasePrice = 500000.00m, Description = "Standard Room with basic amenities", IsActive = true },
                new RoomType { Id = 2, TypeName = "Deluxe", Capacity = 2, BasePrice = 800000.00m, Description = "Deluxe Room with premium comfort", IsActive = true },
                new RoomType { Id = 3, TypeName = "Suite", Capacity = 4, BasePrice = 1200000.00m, Description = "Luxurious Suite with separate living area", IsActive = true },
                new RoomType { Id = 4, TypeName = "Family Room", Capacity = 6, BasePrice = 1500000.00m, Description = "Spacious Room ideal for families", IsActive = true }
            );
        }
    }
}
