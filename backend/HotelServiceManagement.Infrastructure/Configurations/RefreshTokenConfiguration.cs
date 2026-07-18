using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Token)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(rt => rt.ReplacedByToken)
                   .HasMaxLength(200);

            builder.HasIndex(rt => rt.Token).IsUnique();

            // Token vo nghia khi user bi xoa - khac voi cac bang nghiep vu con lai dung Restrict de giu lich su.
            builder.HasOne(rt => rt.User)
                   .WithMany()
                   .HasForeignKey(rt => rt.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
