using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelServiceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestPortalAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuestId = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestAccounts_Guests_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Guests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GuestAccountId = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestRefreshTokens_GuestAccounts_GuestAccountId",
                        column: x => x.GuestAccountId,
                        principalTable: "GuestAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestAccounts_GuestId",
                table: "GuestAccounts",
                column: "GuestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestRefreshTokens_GuestAccountId",
                table: "GuestRefreshTokens",
                column: "GuestAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestRefreshTokens_Token",
                table: "GuestRefreshTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestRefreshTokens");

            migrationBuilder.DropTable(
                name: "GuestAccounts");
        }
    }
}
