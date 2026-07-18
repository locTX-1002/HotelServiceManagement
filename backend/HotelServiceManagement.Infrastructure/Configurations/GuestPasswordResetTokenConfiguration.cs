using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class GuestPasswordResetTokenConfiguration : IEntityTypeConfiguration<GuestPasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<GuestPasswordResetToken> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Token)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.HasIndex(t => t.Token).IsUnique();

            builder.HasOne(t => t.GuestAccount)
                   .WithMany()
                   .HasForeignKey(t => t.GuestAccountId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
