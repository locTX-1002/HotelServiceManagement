using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelServiceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GuestSelfRegisterByPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Guests_IdentityNumber",
                table: "Guests");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityNumber",
                table: "Guests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "GuestPasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GuestAccountId = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestPasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestPasswordResetTokens_GuestAccounts_GuestAccountId",
                        column: x => x.GuestAccountId,
                        principalTable: "GuestAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guests_IdentityNumber",
                table: "Guests",
                column: "IdentityNumber",
                unique: true,
                filter: "[IdentityNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GuestPasswordResetTokens_GuestAccountId",
                table: "GuestPasswordResetTokens",
                column: "GuestAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestPasswordResetTokens_Token",
                table: "GuestPasswordResetTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestPasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_Guests_IdentityNumber",
                table: "Guests");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityNumber",
                table: "Guests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guests_IdentityNumber",
                table: "Guests",
                column: "IdentityNumber",
                unique: true);
        }
    }
}
