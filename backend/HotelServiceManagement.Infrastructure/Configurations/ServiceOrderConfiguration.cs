using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
    {
        public void Configure(EntityTypeBuilder<ServiceOrder> builder)
        {
            builder.HasKey(so => so.Id);
            builder.Property(so => so.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(so => so.TotalAmount).HasPrecision(18, 2);

            // One stay can have many service orders.
            builder.HasOne(so => so.Stay)
                   .WithMany(s => s.ServiceOrders)
                   .HasForeignKey(so => so.StayId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(so => so.CreatedByUser)
                   .WithMany()
                   .HasForeignKey(so => so.CreatedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
