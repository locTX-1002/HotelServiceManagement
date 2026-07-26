using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessObjects.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Roles",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Roles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRole",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "DisplayName", "IsActive", "Module", "PermissionCode" },
                values: new object[,]
                {
                    { 1, "Xem nhân viên", "Xem nhân viên", true, "Người dùng", "user.view" },
                    { 2, "Quản lý nhân viên", "Quản lý nhân viên", true, "Người dùng", "user.manage" },
                    { 3, "Cấu hình phân quyền", "Cấu hình phân quyền", true, "Phân quyền", "permission.manage" },
                    { 4, "Xem nhật ký hệ thống", "Xem nhật ký hệ thống", true, "Nhật ký", "audit.view" },
                    { 5, "Xem phòng", "Xem phòng", true, "Phòng", "room.view" },
                    { 6, "Quản lý phòng", "Quản lý phòng", true, "Phòng", "room.manage" },
                    { 7, "Yêu cầu bảo trì phòng", "Yêu cầu bảo trì phòng", true, "Phòng", "room.maintenance.request" },
                    { 8, "Duyệt bảo trì phòng", "Duyệt bảo trì phòng", true, "Phòng", "room.maintenance.approve" },
                    { 9, "Xem đặt phòng", "Xem đặt phòng", true, "Đặt phòng", "reservation.view" },
                    { 10, "Tạo đặt phòng", "Tạo đặt phòng", true, "Đặt phòng", "reservation.create" },
                    { 11, "Cập nhật đặt phòng", "Cập nhật đặt phòng", true, "Đặt phòng", "reservation.update" },
                    { 12, "Yêu cầu hủy đặt phòng", "Yêu cầu hủy đặt phòng", true, "Đặt phòng", "reservation.cancel.request" },
                    { 13, "Duyệt hủy đặt phòng", "Duyệt hủy đặt phòng", true, "Đặt phòng", "reservation.cancel.approve" },
                    { 14, "Nhận phòng", "Nhận phòng", true, "Lượt ở", "stay.check_in" },
                    { 15, "Gia hạn lượt ở", "Gia hạn lượt ở", true, "Lượt ở", "stay.extend" },
                    { 16, "Trả phòng", "Trả phòng", true, "Lượt ở", "stay.check_out" },
                    { 17, "Xem khách hàng", "Xem khách hàng", true, "Khách hàng", "guest.view" },
                    { 18, "Quản lý khách hàng", "Quản lý khách hàng", true, "Khách hàng", "guest.manage" },
                    { 19, "Quản lý danh mục dịch vụ", "Quản lý danh mục dịch vụ", true, "Dịch vụ", "service.catalog.manage" },
                    { 20, "Tạo đơn dịch vụ", "Tạo đơn dịch vụ", true, "Dịch vụ", "service.order.create" },
                    { 21, "Xử lý đơn dịch vụ", "Xử lý đơn dịch vụ", true, "Dịch vụ", "service.order.process" },
                    { 22, "Quản lý danh mục phụ thu", "Quản lý danh mục phụ thu", true, "Phụ thu", "surcharge.catalog.manage" },
                    { 23, "Thêm phụ thu", "Thêm phụ thu", true, "Phụ thu", "surcharge.add" },
                    { 24, "Quản lý khuyến mãi", "Quản lý khuyến mãi", true, "Khuyến mãi", "promotion.manage" },
                    { 25, "Xem hóa đơn", "Xem hóa đơn", true, "Hóa đơn", "invoice.view" },
                    { 26, "Lập và tính lại hóa đơn", "Lập và tính lại hóa đơn", true, "Hóa đơn", "invoice.prepare" },
                    { 27, "Yêu cầu giảm giá", "Yêu cầu giảm giá", true, "Hóa đơn", "invoice.discount.request" },
                    { 28, "Duyệt giảm giá", "Duyệt giảm giá", true, "Hóa đơn", "invoice.discount.approve" },
                    { 29, "Yêu cầu hủy hóa đơn", "Yêu cầu hủy hóa đơn", true, "Hóa đơn", "invoice.cancel.request" },
                    { 30, "Duyệt hủy hóa đơn", "Duyệt hủy hóa đơn", true, "Hóa đơn", "invoice.cancel.approve" },
                    { 31, "Ghi nhận thanh toán", "Ghi nhận thanh toán", true, "Thanh toán", "payment.record" },
                    { 32, "Yêu cầu hủy giao dịch", "Yêu cầu hủy giao dịch", true, "Thanh toán", "payment.void.request" },
                    { 33, "Duyệt hủy giao dịch", "Duyệt hủy giao dịch", true, "Thanh toán", "payment.void.approve" },
                    { 34, "Xem báo cáo", "Xem báo cáo", true, "Báo cáo", "report.view" },
                    { 35, "Xuất báo cáo", "Xuất báo cáo", true, "Báo cáo", "report.export" }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "DisplayName", "IsActive", "IsSystemRole" },
                values: new object[] { "Quản lý tài khoản, quyền và an toàn hệ thống", "Quản trị viên", true, true });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "DisplayName", "IsActive", "IsSystemRole" },
                values: new object[] { "Quản lý vận hành, báo cáo và phê duyệt", "Quản lý", true, true });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "DisplayName", "IsActive", "IsSystemRole" },
                values: new object[] { "Thực hiện nghiệp vụ tại quầy", "Lễ tân", true, true });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "DisplayName", "IsActive", "IsSystemRole" },
                values: new object[] { "Thực hiện dịch vụ và yêu cầu buồng phòng", "Nhân viên dịch vụ", true, true });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "IsAllowed" },
                values: new object[,]
                {
                    { 1, 1, true },
                    { 2, 1, true },
                    { 3, 1, true },
                    { 4, 1, true },
                    { 5, 2, true },
                    { 6, 2, true },
                    { 8, 2, true },
                    { 9, 2, true },
                    { 13, 2, true },
                    { 19, 2, true },
                    { 21, 2, true },
                    { 22, 2, true },
                    { 24, 2, true },
                    { 25, 2, true },
                    { 28, 2, true },
                    { 30, 2, true },
                    { 33, 2, true },
                    { 34, 2, true },
                    { 35, 2, true },
                    { 5, 3, true },
                    { 7, 3, true },
                    { 9, 3, true },
                    { 10, 3, true },
                    { 11, 3, true },
                    { 12, 3, true },
                    { 14, 3, true },
                    { 15, 3, true },
                    { 16, 3, true },
                    { 17, 3, true },
                    { 18, 3, true },
                    { 20, 3, true },
                    { 23, 3, true },
                    { 25, 3, true },
                    { 26, 3, true },
                    { 27, 3, true },
                    { 29, 3, true },
                    { 31, 3, true },
                    { 32, 3, true },
                    { 5, 4, true },
                    { 7, 4, true },
                    { 21, 4, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionCode",
                table: "Permissions",
                column: "PermissionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsSystemRole",
                table: "Roles");
        }
    }
}
