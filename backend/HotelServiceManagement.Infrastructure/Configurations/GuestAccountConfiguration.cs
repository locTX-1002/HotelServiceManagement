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

            builder.Property(a => a.PasswordHash).HasMaxLength(200);
            builder.Property(a => a.GoogleSubjectId).HasMaxLength(100);

            // Moi Guest chi kich hoat duoc 1 tai khoan dang nhap.
            builder.HasIndex(a => a.GuestId).IsUnique();
            builder.HasIndex(a => a.GoogleSubjectId).IsUnique();

            builder.HasOne(a => a.Guest)
                   .WithOne()
                   .HasForeignKey<GuestAccount>(a => a.GuestId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
