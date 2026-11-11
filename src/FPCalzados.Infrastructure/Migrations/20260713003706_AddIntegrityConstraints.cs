using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPCalzados.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_PriceOverrides_ProductId",
                table: "PriceOverrides");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SaleItems_Prices_NonNegative",
                table: "SaleItems",
                sql: "CAST(UnitPrice AS REAL) >= 0 AND CAST(ManualDiscount AS REAL) >= 0 AND CAST(FinalUnitPrice AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SaleItems_Quantity_Positive",
                table: "SaleItems",
                sql: "Quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SaleItems_ReturnedQuantity_Valid",
                table: "SaleItems",
                sql: "ReturnedQuantity >= 0 AND ReturnedQuantity <= Quantity");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_Color_Size",
                table: "ProductVariants",
                columns: new[] { "ProductId", "Color", "Size" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_Cost_NonNegative",
                table: "ProductVariants",
                sql: "SupplierCostOverride IS NULL OR CAST(SupplierCostOverride AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_Stock_NonNegative",
                table: "ProductVariants",
                sql: "StockQuantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductVariants_Threshold_NonNegative",
                table: "ProductVariants",
                sql: "LowStockThreshold >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_CashPrice_NonNegative",
                table: "Products",
                sql: "CAST(CashPrice AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_SupplierCost_NonNegative",
                table: "Products",
                sql: "CAST(SupplierCost AS REAL) >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_PriceOverrides_ProductId_Color_Size",
                table: "PriceOverrides",
                columns: new[] { "ProductId", "Color", "Size" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PriceOverrides_Price_NonNegative",
                table: "PriceOverrides",
                sql: "CAST(Price AS REAL) >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments",
                sql: "CAST(Amount AS REAL) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SaleItems_Prices_NonNegative",
                table: "SaleItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SaleItems_Quantity_Positive",
                table: "SaleItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SaleItems_ReturnedQuantity_Valid",
                table: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_Color_Size",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_Cost_NonNegative",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_Stock_NonNegative",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductVariants_Threshold_NonNegative",
                table: "ProductVariants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_CashPrice_NonNegative",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_SupplierCost_NonNegative",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PriceOverrides_ProductId_Color_Size",
                table: "PriceOverrides");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PriceOverrides_Price_NonNegative",
                table: "PriceOverrides");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceOverrides_ProductId",
                table: "PriceOverrides",
                column: "ProductId");
        }
    }
}
