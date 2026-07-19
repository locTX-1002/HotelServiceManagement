using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelServiceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HousekeepingRequestType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestType",
                table: "HousekeepingRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Other");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestType",
                table: "HousekeepingRequests");
        }
    }
}
