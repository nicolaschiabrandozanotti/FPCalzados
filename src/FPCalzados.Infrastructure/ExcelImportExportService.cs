using ClosedXML.Excel;
using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

public sealed class ExcelImportExportService
{
    private static readonly string[] StockHeaders =
    [
        "Marca",
        "Modelo",
        "Color",
        "Talle",
        "Cantidad",
        "Costo proveedor",
        "Precio efectivo",
        "Codigo"
    ];

    public void WriteStockTemplate(Stream stream, IEnumerable<StockImportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Stock");
        for (var index = 0; index < StockHeaders.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = StockHeaders[index];
            worksheet.Cell(1, index + 1).Style.Font.Bold = true;
        }

        var rowNumber = 2;
        foreach (var row in rows)
        {
            worksheet.Cell(rowNumber, 1).Value = row.Brand;
            worksheet.Cell(rowNumber, 2).Value = row.Model;
            worksheet.Cell(rowNumber, 3).Value = row.Color;
            worksheet.Cell(rowNumber, 4).Value = row.Size;
            worksheet.Cell(rowNumber, 5).Value = row.Quantity;
            worksheet.Cell(rowNumber, 6).Value = row.SupplierCost;
            worksheet.Cell(rowNumber, 7).Value = row.CashPrice;
            worksheet.Cell(rowNumber, 8).Value = row.Sku;
            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(stream);
    }

    public ImportResult<StockImportRow> ReadStockRows(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var result = new ImportResult<StockImportRow>();
        if (!workbook.Worksheets.TryGetWorksheet("Stock", out var worksheet))
        {
            result.Errors.Add(new ImportError(1, "El archivo debe tener una hoja llamada 'Stock'. Usa la plantilla generada por la app."));
            return result;
        }

        ValidateHeaders(worksheet, result);
        if (!result.IsValid)
        {
            return result;
        }

        var seenVariantKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var brand = worksheet.Cell(rowNumber, 1).GetString().Trim();
            var model = worksheet.Cell(rowNumber, 2).GetString().Trim();
            var color = worksheet.Cell(rowNumber, 3).GetString().Trim();
            var size = worksheet.Cell(rowNumber, 4).GetString().Trim();
            var sku = worksheet.Cell(rowNumber, 8).GetString().Trim();

            if (string.IsNullOrWhiteSpace(brand) && string.IsNullOrWhiteSpace(model))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(brand) ||
                string.IsNullOrWhiteSpace(model))
            {
                result.Errors.Add(new ImportError(rowNumber, "Marca y modelo son obligatorios. Color y talle pueden quedar vacios."));
                continue;
            }

            if (!TryReadInt(worksheet.Cell(rowNumber, 5), out var quantity) || quantity < 0)
            {
                result.Errors.Add(new ImportError(rowNumber, "La cantidad debe ser un numero entero mayor o igual a cero."));
                continue;
            }

            if (!TryReadDecimal(worksheet.Cell(rowNumber, 6), out var supplierCost) || supplierCost < 0m)
            {
                result.Errors.Add(new ImportError(rowNumber, "El costo proveedor debe ser mayor o igual a cero."));
                continue;
            }

            if (!TryReadDecimal(worksheet.Cell(rowNumber, 7), out var cashPrice) || cashPrice < 0m)
            {
                result.Errors.Add(new ImportError(rowNumber, "El precio efectivo debe ser mayor o igual a cero."));
                continue;
            }

            var variantKey = $"{brand}|{model}|{color}|{size}";
            if (!seenVariantKeys.Add(variantKey))
            {
                result.Errors.Add(new ImportError(rowNumber, "Producto/color/talle duplicado dentro del archivo. Unifica la cantidad en una sola fila para evitar doble carga."));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(sku) && !seenSkus.Add(sku))
            {
                result.Errors.Add(new ImportError(rowNumber, "Codigo duplicado dentro del archivo."));
                continue;
            }

            result.Rows.Add(new StockImportRow(
                brand,
                model,
                color,
                size,
                quantity,
                supplierCost,
                cashPrice,
                string.IsNullOrWhiteSpace(sku) ? null : sku,
                rowNumber));
        }

        return result;
    }

    private static void ValidateHeaders(IXLWorksheet worksheet, ImportResult<StockImportRow> result)
    {
        for (var index = 0; index < StockHeaders.Length; index++)
        {
            var actual = worksheet.Cell(1, index + 1).GetString().Trim();
            if (!string.Equals(actual, StockHeaders[index], StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add(new ImportError(1, $"Columna {index + 1}: se esperaba '{StockHeaders[index]}' y se encontro '{actual}'. Usa la plantilla generada por la app."));
            }
        }
    }

    private static bool TryReadInt(IXLCell cell, out int value)
    {
        if (cell.TryGetValue(out value))
        {
            return true;
        }

        return int.TryParse(cell.GetString(), out value);
    }

    private static bool TryReadDecimal(IXLCell cell, out decimal value)
    {
        if (cell.TryGetValue(out value))
        {
            return true;
        }

        return decimal.TryParse(cell.GetString(), out value);
    }
}
