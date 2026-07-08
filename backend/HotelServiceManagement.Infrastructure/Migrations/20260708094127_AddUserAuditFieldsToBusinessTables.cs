using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelServiceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuditFieldsToBusinessTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CheckedInByUserId",
                table: "Stays",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckedOutByUserId",
                table: "Stays",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "ServiceOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceivedByUserId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stays_CheckedInByUserId",
                table: "Stays",
                column: "CheckedInByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Stays_CheckedOutByUserId",
                table: "Stays",
                column: "CheckedOutByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_CreatedByUserId",
                table: "ServiceOrders",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CreatedByUserId",
                table: "Reservations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceivedByUserId",
                table: "Payments",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedByUserId",
                table: "Invoices",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_CreatedByUserId",
                table: "Invoices",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_ReceivedByUserId",
                table: "Payments",
                column: "ReceivedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_CreatedByUserId",
                table: "Reservations",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrders_Users_CreatedByUserId",
                table: "ServiceOrders",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Stays_Users_CheckedInByUserId",
                table: "Stays",
                column: "CheckedInByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Stays_Users_CheckedOutByUserId",
                table: "Stays",
                column: "CheckedOutByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_CreatedByUserId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_ReceivedByUserId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_CreatedByUserId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrders_Users_CreatedByUserId",
                table: "ServiceOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Stays_Users_CheckedInByUserId",
                table: "Stays");

            migrationBuilder.DropForeignKey(
                name: "FK_Stays_Users_CheckedOutByUserId",
                table: "Stays");

            migrationBuilder.DropIndex(
                name: "IX_Stays_CheckedInByUserId",
                table: "Stays");

            migrationBuilder.DropIndex(
                name: "IX_Stays_CheckedOutByUserId",
                table: "Stays");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOrders_CreatedByUserId",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CreatedByUserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ReceivedByUserId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CreatedByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CheckedInByUserId",
                table: "Stays");

            migrationBuilder.DropColumn(
                name: "CheckedOutByUserId",
                table: "Stays");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ReceivedByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Invoices");
        }
    }
}
