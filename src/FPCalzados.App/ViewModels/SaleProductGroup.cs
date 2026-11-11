using FPCalzados.Infrastructure;

namespace FPCalzados.App;

internal sealed record SaleProductGroup(
    int ProductId,
    string Brand,
    string Model,
    string ColorSummary,
    string SizeSummary,
    int TotalStock,
    string SupplierCostSummary,
    string CashPriceSummary,
    IReadOnlyList<InventoryRow> Options);
