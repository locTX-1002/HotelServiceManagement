using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessObjects.Entities;

namespace DataAccessObjects.Configurations
{
    public class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
    {
        public void Configure(EntityTypeBuilder<ServiceItem> builder)
        {
            builder.HasKey(si => si.Id);
            
            builder.Property(si => si.ServiceName)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(si => si.UnitPrice)
                   .HasPrecision(18, 2);

            builder.Property(si => si.IsAvailable)
                   .HasDefaultValue(true);

            builder.HasOne(si => si.ServiceCategory)
                   .WithMany(sc => sc.ServiceItems)
                   .HasForeignKey(si => si.ServiceCategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Seed items - giá thực tế VND, trước đây dùng số nhỏ (2-25) chỉ để test cho gọn, không phải giá thật.
            // ServiceCategoryId 1 = Restaurant, 2 = Laundry
            builder.HasData(
                new ServiceItem { Id = 1, ServiceName = "Breakfast Set", UnitPrice = 80000.00m, ServiceCategoryId = 1, IsAvailable = true },
                new ServiceItem { Id = 2, ServiceName = "Dinner Set", UnitPrice = 150000.00m, ServiceCategoryId = 1, IsAvailable = true },
                new ServiceItem { Id = 3, ServiceName = "Bottled Water", UnitPrice = 15000.00m, ServiceCategoryId = 1, IsAvailable = true },
                new ServiceItem { Id = 4, ServiceName = "Shirt Washing", UnitPrice = 20000.00m, ServiceCategoryId = 2, IsAvailable = true },
                new ServiceItem { Id = 5, ServiceName = "Pants Washing", UnitPrice = 25000.00m, ServiceCategoryId = 2, IsAvailable = true },
                new ServiceItem { Id = 6, ServiceName = "Ironing Service", UnitPrice = 15000.00m, ServiceCategoryId = 2, IsAvailable = true }
            );
        }
    }
}
