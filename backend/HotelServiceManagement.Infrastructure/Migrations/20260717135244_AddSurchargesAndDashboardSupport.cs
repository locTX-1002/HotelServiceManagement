using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelServiceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSurchargesAndDashboardSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SurchargeAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "SurchargeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurchargeItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Surcharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StayId = table.Column<int>(type: "int", nullable: false),
                    SurchargeItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surcharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Surcharges_Stays_StayId",
                        column: x => x.StayId,
                        principalTable: "Stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Surcharges_SurchargeItems_SurchargeItemId",
                        column: x => x.SurchargeItemId,
                        principalTable: "SurchargeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Surcharges_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "SurchargeItems",
                columns: new[] { "Id", "IsActive", "Name", "Unit", "UnitPrice" },
                values: new object[,]
                {
                    { 1, true, "Khăn tắm", "cái", 80000m },
                    { 2, true, "Khăn mặt", "cái", 30000m },
                    { 3, true, "Dép đi trong phòng", "đôi", 40000m },
                    { 4, true, "Remote TV", "cái", 200000m },
                    { 5, true, "Thẻ từ / chìa khóa phòng", "cái", 100000m },
                    { 6, true, "Ấm siêu tốc", "cái", 250000m },
                    { 7, true, "Ly / cốc thủy tinh", "cái", 30000m },
                    { 8, true, "Chăn / ga (ố bẩn nặng)", "bộ", 150000m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurchargeItems_Name",
                table: "SurchargeItems",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Surcharges_CreatedByUserId",
                table: "Surcharges",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Surcharges_StayId",
                table: "Surcharges",
                column: "StayId");

            migrationBuilder.CreateIndex(
                name: "IX_Surcharges_SurchargeItemId",
                table: "Surcharges",
                column: "SurchargeItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Surcharges");

            migrationBuilder.DropTable(
                name: "SurchargeItems");

            migrationBuilder.DropColumn(
                name: "SurchargeAmount",
                table: "Invoices");
        }
    }
}
