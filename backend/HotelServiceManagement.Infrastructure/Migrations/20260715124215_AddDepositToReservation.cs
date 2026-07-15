using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelServiceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositToReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "Reservations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepositPaidAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositPaymentMethod",
                table: "Reservations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservation_DepositAmount_NonNegative",
                table: "Reservations",
                sql: "[DepositAmount] IS NULL OR [DepositAmount] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservation_DepositAmount_NonNegative",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DepositPaidAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DepositPaymentMethod",
                table: "Reservations");
        }
    }
}
