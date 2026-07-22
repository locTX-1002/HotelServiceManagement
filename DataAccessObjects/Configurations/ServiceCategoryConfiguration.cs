using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessObjects.Entities;

namespace DataAccessObjects.Configurations
{
    public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
    {
        public void Configure(EntityTypeBuilder<ServiceCategory> builder)
        {
            builder.HasKey(sc => sc.Id);
            builder.Property(sc => sc.CategoryName).IsRequired().HasMaxLength(100);
            builder.Property(sc => sc.IsActive).HasDefaultValue(true);

            // Seed Categories
            builder.HasData(
                new ServiceCategory { Id = 1, CategoryName = "Restaurant", IsActive = true },
                new ServiceCategory { Id = 2, CategoryName = "Laundry", IsActive = true }
            );
        }
    }
}
