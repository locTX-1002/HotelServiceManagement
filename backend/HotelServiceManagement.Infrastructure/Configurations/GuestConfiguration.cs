using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Configurations
{
    public class GuestConfiguration : IEntityTypeConfiguration<Guest>
    {
        public void Configure(EntityTypeBuilder<Guest> builder)
        {
            builder.HasKey(g => g.Id);
            builder.Property(g => g.FullName).IsRequired().HasMaxLength(100);
            builder.Property(g => g.Email).HasMaxLength(150);
            builder.Property(g => g.PhoneNumber).IsRequired().HasMaxLength(20);
            builder.Property(g => g.IdentityNumber).IsRequired().HasMaxLength(50);
            builder.HasIndex(g => g.IdentityNumber).IsUnique();
        }
    }
}
