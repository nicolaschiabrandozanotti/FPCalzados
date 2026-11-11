using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPCalzados.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryDateKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedDateKey",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaidDateKey",
                table: "Payments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE Sales
                SET CreatedDateKey = CAST(REPLACE(SUBSTR(CreatedAt, 1, 10), '-', '') AS INTEGER);
                UPDATE Payments
                SET PaidDateKey = CAST(REPLACE(SUBSTR(PaidAt, 1, 10), '-', '') AS INTEGER);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CreatedDateKey",
                table: "Sales",
                column: "CreatedDateKey");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaidDateKey",
                table: "Payments",
                column: "PaidDateKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_CreatedDateKey",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaidDateKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CreatedDateKey",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PaidDateKey",
                table: "Payments");
        }
    }
}
