using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Amount).HasPrecision(18, 2);
            builder.Property(p => p.PaymentMethod).HasConversion<string>().HasMaxLength(50);
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(p => p.TransactionId).HasMaxLength(100);

            // One invoice can have many payments.
            builder.HasOne(p => p.Invoice)
                   .WithMany(i => i.Payments)
                   .HasForeignKey(p => p.InvoiceId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
