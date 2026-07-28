using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObjects.Migrations
{
    /// <inheritdoc />
    public partial class LinkInvoicePromotionAndPersistDiscountBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVipDiscountApplied",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ManualDiscountAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PromotionId",
                table: "Invoices",
                type: "int",
                nullable: true);

            // Chuyển dữ liệu cũ: VIP10/TUNHAP từng bị ghép vào PromotionCode.
            migrationBuilder.Sql(
                @"
UPDATE i
SET i.IsVipDiscountApplied = 1
FROM Invoices AS i
WHERE CHARINDEX(
    '+VIP10+',
    '+' + UPPER(ISNULL(i.PromotionCode, '')) + '+') > 0;

;WITH LatestApprovedManualDiscount AS
(
    SELECT
        ar.TargetId AS StayId,
        ar.RequestedValue,
        ROW_NUMBER() OVER
        (
            PARTITION BY ar.TargetId
            ORDER BY ISNULL(ar.ReviewedAt, ar.RequestedAt) DESC, ar.Id DESC
        ) AS RowNumber
    FROM ApprovalRequests AS ar
    WHERE ar.RequestType = 'InvoiceDiscount'
      AND ar.Status = 'Approved'
      AND ar.RequestedValue > 0
)
UPDATE i
SET i.ManualDiscountAmount =
    CASE
        WHEN d.RequestedValue > i.DiscountAmount THEN i.DiscountAmount
        ELSE d.RequestedValue
    END
FROM Invoices AS i
INNER JOIN LatestApprovedManualDiscount AS d
    ON d.StayId = i.StayId
   AND d.RowNumber = 1
WHERE CHARINDEX(
    '+TUNHAP+',
    '+' + UPPER(ISNULL(i.PromotionCode, '')) + '+') > 0;

UPDATE i
SET
    i.PromotionId = matched.Id,
    i.PromotionCode = matched.Code
FROM Invoices AS i
OUTER APPLY
(
    SELECT TOP (1)
        p.Id,
        p.Code
    FROM Promotions AS p
    WHERE CHARINDEX(
        '+' + UPPER(p.Code) + '+',
        '+' + UPPER(ISNULL(i.PromotionCode, '')) + '+') > 0
    ORDER BY p.Id
) AS matched
WHERE matched.Id IS NOT NULL;

UPDATE Invoices
SET PromotionCode = NULL
WHERE PromotionId IS NULL;
");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PromotionId",
                table: "Invoices",
                column: "PromotionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Invoice_ManualDiscountAmount_NonNegative",
                table: "Invoices",
                sql: "[ManualDiscountAmount] >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Promotions_PromotionId",
                table: "Invoices",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Promotions_PromotionId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PromotionId",
                table: "Invoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Invoice_ManualDiscountAmount_NonNegative",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsVipDiscountApplied",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ManualDiscountAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "Invoices");
        }
    }
}
