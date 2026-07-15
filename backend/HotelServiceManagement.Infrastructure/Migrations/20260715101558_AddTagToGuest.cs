using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelServiceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTagToGuest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "Guests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "TagNote",
                table: "Guests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tag",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "TagNote",
                table: "Guests");
        }
    }
}
