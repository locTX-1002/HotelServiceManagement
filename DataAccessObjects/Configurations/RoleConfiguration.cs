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
            builder.Property(r => r.DisplayName).IsRequired().HasMaxLength(100);
            builder.Property(r => r.Description).HasMaxLength(300);
            builder.HasIndex(r => r.RoleName).IsUnique();

            // Seed Roles
            builder.HasData(
                new Role { Id = 1, RoleName = RoleNames.Admin, DisplayName = "Quản trị viên", Description = "Quản lý tài khoản, quyền và an toàn hệ thống", IsSystemRole = true, IsActive = true },
                new Role { Id = 2, RoleName = RoleNames.Manager, DisplayName = "Quản lý", Description = "Quản lý vận hành, báo cáo và phê duyệt", IsSystemRole = true, IsActive = true },
                new Role { Id = 3, RoleName = RoleNames.Receptionist, DisplayName = "Lễ tân", Description = "Thực hiện nghiệp vụ tại quầy", IsSystemRole = true, IsActive = true },
                new Role { Id = 4, RoleName = RoleNames.ServiceStaff, DisplayName = "Nhân viên dịch vụ", Description = "Thực hiện dịch vụ và yêu cầu buồng phòng", IsSystemRole = true, IsActive = true }
            );
        }
    }
}
