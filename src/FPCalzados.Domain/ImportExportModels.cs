namespace FPCalzados.Domain;

public sealed record StockImportRow(
    string Brand,
    string Model,
    string Color,
    string Size,
    int Quantity,
    decimal SupplierCost,
    decimal CashPrice,
    string? Sku,
    int SourceRowNumber = 0);

public sealed record ImportError(int RowNumber, string Message);

public sealed class ImportResult<T>
{
    public List<T> Rows { get; } = [];
    public List<ImportError> Errors { get; } = [];
    public bool IsValid => Errors.Count == 0;
}
