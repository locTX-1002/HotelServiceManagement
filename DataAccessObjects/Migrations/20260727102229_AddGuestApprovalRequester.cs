using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObjects.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestApprovalRequester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RequestedByUserId",
                table: "ApprovalRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "RequestedByGuestId",
                table: "ApprovalRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestedByGuestId",
                table: "ApprovalRequests",
                column: "RequestedByGuestId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalRequest_Requester",
                table: "ApprovalRequests",
                sql: "([RequestedByUserId] IS NOT NULL AND [RequestedByGuestId] IS NULL) OR ([RequestedByUserId] IS NULL AND [RequestedByGuestId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_Guests_RequestedByGuestId",
                table: "ApprovalRequests",
                column: "RequestedByGuestId",
                principalTable: "Guests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_Guests_RequestedByGuestId",
                table: "ApprovalRequests");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_RequestedByGuestId",
                table: "ApprovalRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalRequest_Requester",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "RequestedByGuestId",
                table: "ApprovalRequests");

            migrationBuilder.AlterColumn<int>(
                name: "RequestedByUserId",
                table: "ApprovalRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
