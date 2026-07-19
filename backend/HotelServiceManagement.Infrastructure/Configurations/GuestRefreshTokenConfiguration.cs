using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class GuestRefreshTokenConfiguration : IEntityTypeConfiguration<GuestRefreshToken>
    {
        public void Configure(EntityTypeBuilder<GuestRefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Token)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(rt => rt.ReplacedByToken)
                   .HasMaxLength(200);

            builder.HasIndex(rt => rt.Token).IsUnique();

            builder.HasOne(rt => rt.GuestAccount)
                   .WithMany()
                   .HasForeignKey(rt => rt.GuestAccountId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
