using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelServiceManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToGuestIdentityNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dọn dữ liệu trùng IdentityNumber có sẵn (race condition trước khi fix để lại) trước khi
            // tạo unique index, nếu không migration sẽ fail. Giữ lại bản ghi có Reservation gắn vào
            // (dữ liệu nghiệp vụ thật), chỉ xoá các bản ghi trùng mồ côi (ReservationCount = 0).
            migrationBuilder.Sql(@"
                ;WITH Ranked AS (
                    SELECT g.Id,
                           ROW_NUMBER() OVER (
                               PARTITION BY g.IdentityNumber
                               ORDER BY (SELECT COUNT(*) FROM Reservations r WHERE r.GuestId = g.Id) DESC, g.Id ASC
                           ) AS rn
                    FROM Guests g
                )
                DELETE FROM Guests WHERE Id IN (SELECT Id FROM Ranked WHERE rn > 1);
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_IdentityNumber",
                table: "Guests",
                column: "IdentityNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Guests_IdentityNumber",
                table: "Guests");
        }
    }
}
