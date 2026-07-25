using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessObjects.Entities;

namespace DataAccessObjects.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.RoleName).IsRequired().HasMaxLength(50);
            builder.HasIndex(r => r.RoleName).IsUnique();

            // Seed Roles
            builder.HasData(
                new Role { Id = 1, RoleName = RoleNames.Admin },
                new Role { Id = 2, RoleName = RoleNames.Manager },
                new Role { Id = 3, RoleName = RoleNames.Receptionist },
                new Role { Id = 4, RoleName = RoleNames.ServiceStaff }
            );
        }
    }
}
