using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelServiceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingUniqueAndRestrictBusinessDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Invoices_InvoiceId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrderDetails_ServiceOrders_ServiceOrderId",
                table: "ServiceOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrders_Stays_StayId",
                table: "ServiceOrders");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Invoices_InvoiceId",
                table: "Payments",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrderDetails_ServiceOrders_ServiceOrderId",
                table: "ServiceOrderDetails",
                column: "ServiceOrderId",
                principalTable: "ServiceOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrders_Stays_StayId",
                table: "ServiceOrders",
                column: "StayId",
                principalTable: "Stays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Invoices_InvoiceId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrderDetails_ServiceOrders_ServiceOrderId",
                table: "ServiceOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrders_Stays_StayId",
                table: "ServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_Roles_RoleName",
                table: "Roles");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Invoices_InvoiceId",
                table: "Payments",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrderDetails_ServiceOrders_ServiceOrderId",
                table: "ServiceOrderDetails",
                column: "ServiceOrderId",
                principalTable: "ServiceOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrders_Stays_StayId",
                table: "ServiceOrders",
                column: "StayId",
                principalTable: "Stays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
