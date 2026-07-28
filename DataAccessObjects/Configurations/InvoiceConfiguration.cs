using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessObjects.Entities;

namespace DataAccessObjects.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.RoomCharge).HasPrecision(18, 2);
            builder.Property(i => i.ServiceCharge).HasPrecision(18, 2);
            builder.Property(i => i.SurchargeAmount).HasPrecision(18, 2);
            builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
            builder.Property(i => i.PromotionCode).HasMaxLength(30);
            builder.Property(i => i.ManualDiscountAmount)
                   .HasPrecision(18, 2)
                   .HasDefaultValue(0m);
            builder.Property(i => i.IsVipDiscountApplied)
                   .HasDefaultValue(false);
            builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
            builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(50);

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Invoice_ManualDiscountAmount_NonNegative",
                    "[ManualDiscountAmount] >= 0");
            });

            // One stay can generate zero or one invoice.
            builder.HasOne(i => i.Stay)
                   .WithOne(s => s.Invoice)
                   .HasForeignKey<Invoice>(i => i.StayId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.CreatedByUser)
                   .WithMany()
                   .HasForeignKey(i => i.CreatedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Promotion là danh mục thật; VIP và giảm tay được lưu ở các cột riêng.
            builder.HasOne(i => i.Promotion)
                   .WithMany(p => p.Invoices)
                   .HasForeignKey(i => i.PromotionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(i => i.PromotionId);
        }
    }
}
