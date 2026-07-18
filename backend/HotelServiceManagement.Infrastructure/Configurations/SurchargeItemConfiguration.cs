using HotelServiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelServiceManagement.Infrastructure.Configurations;

public class SurchargeItemConfiguration : IEntityTypeConfiguration<SurchargeItem>
{
    public void Configure(EntityTypeBuilder<SurchargeItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(i => i.Name).IsUnique();
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Unit).IsRequired().HasMaxLength(20);
        builder.Property(i => i.IsActive).HasDefaultValue(true);

        builder.HasData(
            new SurchargeItem { Id = 1, Name = "Khăn tắm", Unit = "cái", UnitPrice = 80000m, IsActive = true },
            new SurchargeItem { Id = 2, Name = "Khăn mặt", Unit = "cái", UnitPrice = 30000m, IsActive = true },
            new SurchargeItem { Id = 3, Name = "Dép đi trong phòng", Unit = "đôi", UnitPrice = 40000m, IsActive = true },
            new SurchargeItem { Id = 4, Name = "Remote TV", Unit = "cái", UnitPrice = 200000m, IsActive = true },
            new SurchargeItem { Id = 5, Name = "Thẻ từ / chìa khóa phòng", Unit = "cái", UnitPrice = 100000m, IsActive = true },
            new SurchargeItem { Id = 6, Name = "Ấm siêu tốc", Unit = "cái", UnitPrice = 250000m, IsActive = true },
            new SurchargeItem { Id = 7, Name = "Ly / cốc thủy tinh", Unit = "cái", UnitPrice = 30000m, IsActive = true },
            new SurchargeItem { Id = 8, Name = "Chăn / ga (ố bẩn nặng)", Unit = "bộ", UnitPrice = 150000m, IsActive = true });
    }
}
