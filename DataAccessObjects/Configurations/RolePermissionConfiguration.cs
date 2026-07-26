using BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccessObjects.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(x => new { x.RoleId, x.PermissionId });
        builder.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId);
        builder.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId);

        builder.HasData(
            // Admin: quan tri danh tinh va an toan he thong.
            RP(1, 1), RP(1, 2), RP(1, 3), RP(1, 4),

            // Manager: cau hinh van hanh, theo doi va phe duyet.
            RP(2, 5), RP(2, 6), RP(2, 8), RP(2, 9), RP(2, 13),
            RP(2, 19), RP(2, 21), RP(2, 22), RP(2, 24), RP(2, 25),
            RP(2, 28), RP(2, 30), RP(2, 33), RP(2, 34), RP(2, 35),

            // Receptionist: nghiep vu tai quay va gui yeu cau nhay cam.
            RP(3, 5), RP(3, 7), RP(3, 9), RP(3, 10), RP(3, 11), RP(3, 12),
            RP(3, 14), RP(3, 15), RP(3, 16), RP(3, 17), RP(3, 18), RP(3, 20),
            RP(3, 23), RP(3, 25), RP(3, 26), RP(3, 27), RP(3, 29), RP(3, 31), RP(3, 32),

            // ServiceStaff: thuc hien dich vu va yeu cau buong phong.
            RP(4, 5), RP(4, 7), RP(4, 21)
        );
    }

    private static RolePermission RP(int roleId, int permissionId) => new()
    {
        RoleId = roleId,
        PermissionId = permissionId,
        IsAllowed = true
    };
}
