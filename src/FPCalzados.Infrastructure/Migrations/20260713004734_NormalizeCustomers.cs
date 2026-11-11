using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPCalzados.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedDocument",
                table: "Customers",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Customers",
                type: "TEXT",
                maxLength: 241,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPhone",
                table: "Customers",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Customers
                SET NormalizedName = UPPER(TRIM(FirstName || ' ' || LastName)),
                    NormalizedPhone = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Phone, ' ', ''), '-', ''), '(', ''), ')', ''), '+', ''), '.', ''),
                    NormalizedDocument = NULLIF(UPPER(REPLACE(REPLACE(REPLACE(TRIM(COALESCE(DocumentNumber, '')), ' ', ''), '-', ''), '.', '')), '');

                UPDATE Sales
                SET CustomerId = (
                    SELECT MIN(other.Id)
                    FROM Customers other
                    JOIN Customers current ON current.Id = Sales.CustomerId
                    WHERE (current.NormalizedDocument IS NOT NULL AND other.NormalizedDocument = current.NormalizedDocument)
                       OR (current.NormalizedDocument IS NULL AND other.NormalizedDocument IS NULL
                           AND other.NormalizedName = current.NormalizedName
                           AND other.NormalizedPhone = current.NormalizedPhone)
                )
                WHERE CustomerId IS NOT NULL;

                DELETE FROM Customers
                WHERE EXISTS (
                    SELECT 1 FROM Customers other
                    WHERE other.Id < Customers.Id
                      AND ((Customers.NormalizedDocument IS NOT NULL AND other.NormalizedDocument = Customers.NormalizedDocument)
                        OR (Customers.NormalizedDocument IS NULL AND other.NormalizedDocument IS NULL
                            AND other.NormalizedName = Customers.NormalizedName
                            AND other.NormalizedPhone = Customers.NormalizedPhone))
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NormalizedDocument",
                table: "Customers",
                column: "NormalizedDocument",
                unique: true,
                filter: "NormalizedDocument IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NormalizedName",
                table: "Customers",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NormalizedPhone",
                table: "Customers",
                column: "NormalizedPhone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_NormalizedDocument",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_NormalizedName",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_NormalizedPhone",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NormalizedDocument",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NormalizedPhone",
                table: "Customers");
        }
    }
}
