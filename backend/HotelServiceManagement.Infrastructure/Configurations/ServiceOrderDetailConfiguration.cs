using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class ServiceOrderDetailConfiguration : IEntityTypeConfiguration<ServiceOrderDetail>
    {
        public void Configure(EntityTypeBuilder<ServiceOrderDetail> builder)
        {
            builder.HasKey(sod => sod.Id);
            builder.Property(sod => sod.UnitPrice).HasPrecision(18, 2);
            builder.Property(sod => sod.Subtotal).HasPrecision(18, 2);

            // One service order can have many details.
            builder.HasOne(sod => sod.ServiceOrder)
                   .WithMany(so => so.OrderDetails)
                   .HasForeignKey(sod => sod.ServiceOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sod => sod.ServiceItem)
                   .WithMany()
                   .HasForeignKey(sod => sod.ServiceItemId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
