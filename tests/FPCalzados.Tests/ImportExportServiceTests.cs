using FPCalzados.Domain;
using FPCalzados.Infrastructure;

namespace FPCalzados.Tests;

public sealed class ImportExportServiceTests
{
    [Fact]
    public void Stock_template_round_trips_valid_rows_from_xlsx()
    {
        var service = new ExcelImportExportService();
        using var stream = new MemoryStream();

        service.WriteStockTemplate(stream, [new StockImportRow("Kioshi", "Roma", "Negro", "39", 3, 20000m, 40000m, "ABC123")]);
        stream.Position = 0;
        var result = service.ReadStockRows(stream);

        Assert.True(result.IsValid);
        Assert.Single(result.Rows);
        Assert.Equal("Kioshi", result.Rows[0].Brand);
        Assert.Equal(3, result.Rows[0].Quantity);
    }
}
