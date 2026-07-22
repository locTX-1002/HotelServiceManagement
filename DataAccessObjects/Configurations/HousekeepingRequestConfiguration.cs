using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessObjects.Entities;
using BusinessObjects.Enums;

namespace DataAccessObjects.Configurations
{
    public class HousekeepingRequestConfiguration : IEntityTypeConfiguration<HousekeepingRequest>
    {
        public void Configure(EntityTypeBuilder<HousekeepingRequest> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Note).HasMaxLength(300);
            builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(r => r.RequestType).HasConversion<string>().HasMaxLength(20)
                   .HasDefaultValue(HousekeepingRequestType.Other);

            builder.HasOne(r => r.Stay)
                   .WithMany()
                   .HasForeignKey(r => r.StayId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.HandledByUser)
                   .WithMany()
                   .HasForeignKey(r => r.HandledByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => new { r.Status, r.RequestedAt });
        }
    }
}
