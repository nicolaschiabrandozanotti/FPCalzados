using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

public sealed partial class ApplicationStore
{
    public void ImportStockRows(IEnumerable<StockImportRow> rows)
    {
        RequireAdmin();
        var materializedRows = rows.ToList();
        inventory.ImportStockRows(materializedRows, currentRole);
    }
}
