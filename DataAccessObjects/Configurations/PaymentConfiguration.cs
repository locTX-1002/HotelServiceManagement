using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessObjects.Entities;

namespace DataAccessObjects.Configurations
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

            // Cho phep nhieu payment tien mat co TransactionId NULL, nhung ma giao dich
            // chuyen khoan phai duy nhat de tranh ghi nhan cung mot giao dich hai lan.
            builder.HasIndex(p => p.TransactionId)
                   .IsUnique()
                   .HasFilter("[TransactionId] IS NOT NULL");

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Payment_Amount_Positive", "[Amount] > 0"));

            // One invoice can have many payments.
            builder.HasOne(p => p.Invoice)
                   .WithMany(i => i.Payments)
                   .HasForeignKey(p => p.InvoiceId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ReceivedByUser)
                   .WithMany()
                   .HasForeignKey(p => p.ReceivedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
