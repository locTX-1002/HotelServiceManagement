using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class GuestAccountConfiguration : IEntityTypeConfiguration<GuestAccount>
    {
        public void Configure(EntityTypeBuilder<GuestAccount> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.PasswordHash)
                   .IsRequired()
                   .HasMaxLength(200);

            // Moi Guest chi kich hoat duoc 1 tai khoan dang nhap.
            builder.HasIndex(a => a.GuestId).IsUnique();

            builder.HasOne(a => a.Guest)
                   .WithOne()
                   .HasForeignKey<GuestAccount>(a => a.GuestId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
