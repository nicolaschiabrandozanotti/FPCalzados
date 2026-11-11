using ClosedXML.Excel;
using FPCalzados.Domain;
using Microsoft.EntityFrameworkCore;

namespace FPCalzados.Infrastructure;

internal sealed class DashboardReportApplicationService(
    AppDbContextFactory factory,
    IClock clock,
    Func<IReadOnlyList<InventoryRow>> getInventory,
    Func<int?, int?, int, IReadOnlyList<StockMovementRow>> getStockMovements)
{
    public DashboardSummary GetDashboard(DateOnly from, DateOnly to)
    {
        using var db = factory.CreateDbContext();
        var sales = SalesApplicationService.LoadSales(db, from, to);
        var inventory = db.ProductVariants.Include(item => item.Product).AsNoTracking().ToList();
        var alerts = new AlertService().BuildAlerts(inventory, sales, DateOnly.FromDateTime(clock.Now.DateTime));
        var topProducts = sales
            .SelectMany(sale => sale.Items.Select(item => new
            {
                item.Variant.Product.Brand,
                item.Variant.Product.Model,
                Quantity = item.Quantity - item.ReturnedQuantity,
                Total = (item.Quantity - item.ReturnedQuantity) * item.FinalUnitPrice
            }))
            .GroupBy(item => new { item.Brand, item.Model })
            .Select(group => new TopProductRow(
                group.Key.Brand,
                group.Key.Model,
                group.Sum(item => item.Quantity),
                group.Sum(item => item.Total)))
            .OrderByDescending(item => item.Quantity)
            .ThenByDescending(item => item.NetTotal)
            .Take(10)
            .ToList();

        return new DashboardSummary(
            sales.Sum(item => item.GrossTotal),
            sales.Sum(item => item.NetTotal),
            sales.Sum(item => item.PaidTotal),
            sales.Sum(item => item.BalanceDue),
            sales.Sum(item => item.ReturnedTotal),
            inventory.Count(item => item.StockQuantity <= item.LowStockThreshold),
            sales.Count(item => item.BalanceDue > 0m && item.DueDate is not null && item.DueDate.Value < DateOnly.FromDateTime(clock.Now.DateTime)),
            topProducts,
            alerts);
    }

    public CashRegisterSummary GetCashRegister(DateOnly from, DateOnly to)
    {
        using var db = factory.CreateDbContext();
        var sales = SalesApplicationService.LoadSales(db, from, to);
        var session = GetSessionForPeriod(db, from, to);
        return new CashRegisterSummary(
            sales.Count,
            sales.Sum(item => item.GrossTotal),
            sales.Sum(item => item.NetTotal),
            sales.SelectMany(item => item.Payments).Where(item => item.Method == PaymentMethod.Cash).Sum(item => item.Amount),
            sales.SelectMany(item => item.Payments).Where(item => item.Method == PaymentMethod.Debit).Sum(item => item.Amount),
            sales.SelectMany(item => item.Payments).Where(item => item.Method == PaymentMethod.Credit).Sum(item => item.Amount),
            sales.Sum(item => item.PaidTotal),
            sales.Sum(item => item.BalanceDue),
            sales.Sum(item => item.ReturnedTotal),
            sales.SelectMany(item => item.Returns).Sum(item => item.CashToReturn),
            session?.Id,
            session?.OpenedAt,
            session?.ClosedAt,
            session?.OpeningCash ?? 0m,
            session?.ClosingCash,
            session?.Notes);
    }

    public CashRegisterSession OpenCashRegister(decimal openingCash, string? notes, UserRole? role)
    {
        using var db = factory.CreateDbContext();
        if (db.CashRegisterSessions.Any(item => item.ClosedAt == null))
        {
            throw new InvalidOperationException("Ya hay una caja abierta. Cerrala antes de abrir otra.");
        }

        var session = new CashRegisterSession
        {
            OpenedAt = clock.Now,
            OpeningCash = openingCash,
            Notes = notes
        };
        db.CashRegisterSessions.Add(session);
        AuditWriter.Add(db, clock, role, "OpenCashRegister", "CashRegisterSession", null, $"Apertura de caja con {openingCash:C0}.");
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException exception) when (db.CashRegisterSessions.Any(item => item.ClosedAt == null))
        {
            throw new InvalidOperationException("Ya hay una caja abierta. Actualiza los datos antes de continuar.", exception);
        }
        return session;
    }

    public CashRegisterSession CloseCashRegister(decimal closingCash, string? notes, UserRole? role)
    {
        using var db = factory.CreateDbContext();
        var session = db.CashRegisterSessions
            .OrderByDescending(item => item.Id)
            .FirstOrDefault(item => item.ClosedAt == null)
            ?? throw new InvalidOperationException("No hay una caja abierta para cerrar.");

        session.ClosedAt = clock.Now;
        session.ClosingCash = closingCash;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            session.Notes = string.IsNullOrWhiteSpace(session.Notes)
                ? notes.Trim()
                : $"{session.Notes.Trim()} | Cierre: {notes.Trim()}";
        }

        session.Version++;
        AuditWriter.Add(db, clock, role, "CloseCashRegister", "CashRegisterSession", session.Id.ToString(), $"Cierre de caja con {closingCash:C0}.");
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InvalidOperationException("La caja ya fue modificada o cerrada por otra operación.", exception);
        }
        return session;
    }

    private static CashRegisterSession? GetSessionForPeriod(AppDbContext db, DateOnly from, DateOnly to)
    {
        var openSession = db.CashRegisterSessions
            .AsNoTracking()
            .OrderByDescending(item => item.Id)
            .FirstOrDefault(item => item.ClosedAt == null);
        if (openSession is not null)
        {
            return openSession;
        }

        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.ToDateTime(TimeOnly.MaxValue);
        return db.CashRegisterSessions
            .AsNoTracking()
            .AsEnumerable()
            .Where(item => item.OpenedAt >= start && item.OpenedAt <= end)
            .OrderByDescending(item => item.Id)
            .FirstOrDefault();
    }

    public ProfitabilitySummary GetProfitability(DateOnly from, DateOnly to)
    {
        using var db = factory.CreateDbContext();
        var sales = SalesApplicationService.LoadSales(db, from, to);
        var rows = sales
            .SelectMany(sale => sale.Items.Select(item =>
            {
                var quantity = item.Quantity - item.ReturnedQuantity;
                var netSales = quantity * item.FinalUnitPrice;
                var unitCost = item.SupplierCostSnapshot > 0m
                    ? item.SupplierCostSnapshot
                    : item.Variant.SupplierCostOverride ?? item.Variant.Product.SupplierCost;
                var estimatedCost = quantity * unitCost;
                var grossProfit = netSales - estimatedCost;
                return new
                {
                    item.Variant.Product.Brand,
                    item.Variant.Product.Model,
                    Quantity = quantity,
                    NetSales = netSales,
                    EstimatedCost = estimatedCost,
                    GrossProfit = grossProfit
                };
            }))
            .Where(item => item.Quantity > 0)
            .ToList();

        var products = rows
            .GroupBy(item => new { item.Brand, item.Model })
            .Select(group =>
            {
                var netSales = group.Sum(item => item.NetSales);
                var estimatedCost = group.Sum(item => item.EstimatedCost);
                var grossProfit = group.Sum(item => item.GrossProfit);
                return new ProfitabilityProductRow(
                    group.Key.Brand,
                    group.Key.Model,
                    group.Sum(item => item.Quantity),
                    netSales,
                    estimatedCost,
                    grossProfit,
                    CalculateMargin(netSales, grossProfit));
            })
            .OrderByDescending(item => item.GrossProfit)
            .ThenByDescending(item => item.NetSales)
            .ToList();

        var totalNetSales = rows.Sum(item => item.NetSales);
        var totalEstimatedCost = rows.Sum(item => item.EstimatedCost);
        var totalGrossProfit = rows.Sum(item => item.GrossProfit);
        return new ProfitabilitySummary(
            totalNetSales,
            totalEstimatedCost,
            totalGrossProfit,
            CalculateMargin(totalNetSales, totalGrossProfit),
            products);
    }

    public string ExportCashRegisterReport(string directory, DateOnly from, DateOnly to)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"cierre-caja-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx");
        var summary = GetCashRegister(from, to);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Cierre");
        worksheet.Cell(1, 1).Value = "Concepto";
        worksheet.Cell(1, 2).Value = "Importe";
        worksheet.Range(1, 1, 1, 2).Style.Font.Bold = true;

        var rows = new (string Concept, object Value)[]
        {
            ("Desde", from.ToString("dd/MM/yyyy")),
            ("Hasta", to.ToString("dd/MM/yyyy")),
            ("Ventas", summary.SalesCount),
            ("Bruto", summary.GrossSales),
            ("Neto", summary.NetSales),
            ("Efectivo", summary.CashTotal),
            ("Debito", summary.DebitTotal),
            ("Credito", summary.CreditTotal),
            ("Cobrado", summary.PaidTotal),
            ("Pendiente", summary.PendingTotal),
            ("Devuelto", summary.ReturnedTotal),
            ("Dinero a devolver", summary.CashToReturn),
            ("Caja inicial", summary.OpeningCash),
            ("Movimiento efectivo", summary.CashMovement),
            ("Caja esperada", summary.ExpectedCash),
            ("Caja real", summary.ClosingCash?.ToString("0.##") ?? "Sin cerrar"),
            ("Diferencia", summary.Difference?.ToString("0.##") ?? "Sin cerrar"),
            ("Estado", summary.IsOpen ? "Abierta" : summary.HasSession ? "Cerrada" : "Sin apertura")
        };

        for (var index = 0; index < rows.Length; index++)
        {
            worksheet.Cell(index + 2, 1).Value = rows[index].Concept;
            worksheet.Cell(index + 2, 2).Value = XLCellValue.FromObject(rows[index].Value);
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
        return path;
    }

    public string ExportProfitabilityReport(string directory, DateOnly from, DateOnly to)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"rentabilidad-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx");
        var summary = GetProfitability(from, to);
        using var workbook = new XLWorkbook();
        var summarySheet = workbook.AddWorksheet("Resumen");
        summarySheet.Cell(1, 1).Value = "Concepto";
        summarySheet.Cell(1, 2).Value = "Valor";
        summarySheet.Range(1, 1, 1, 2).Style.Font.Bold = true;
        var summaryRows = new (string Concept, object Value)[]
        {
            ("Desde", from.ToString("dd/MM/yyyy")),
            ("Hasta", to.ToString("dd/MM/yyyy")),
            ("Venta neta", summary.NetSales),
            ("Costo estimado", summary.EstimatedCost),
            ("Ganancia bruta", summary.GrossProfit),
            ("Margen %", summary.MarginPercent)
        };

        for (var index = 0; index < summaryRows.Length; index++)
        {
            summarySheet.Cell(index + 2, 1).Value = summaryRows[index].Concept;
            summarySheet.Cell(index + 2, 2).Value = XLCellValue.FromObject(summaryRows[index].Value);
        }

        var productsSheet = workbook.AddWorksheet("Por producto");
        var headers = new[] { "Marca", "Modelo", "Cantidad", "Venta neta", "Costo estimado", "Ganancia bruta", "Margen %" };
        for (var index = 0; index < headers.Length; index++)
        {
            productsSheet.Cell(1, index + 1).Value = headers[index];
            productsSheet.Cell(1, index + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var item in summary.Products)
        {
            productsSheet.Cell(row, 1).Value = item.Brand;
            productsSheet.Cell(row, 2).Value = item.Model;
            productsSheet.Cell(row, 3).Value = item.Quantity;
            productsSheet.Cell(row, 4).Value = item.NetSales;
            productsSheet.Cell(row, 5).Value = item.EstimatedCost;
            productsSheet.Cell(row, 6).Value = item.GrossProfit;
            productsSheet.Cell(row, 7).Value = item.MarginPercent;
            row++;
        }

        summarySheet.Columns().AdjustToContents();
        productsSheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
        return path;
    }

    public string ExportSalesPdfReport(string directory, DateOnly from, DateOnly to, IReadOnlyList<SaleRow> sales)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"ventas-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
        var lines = new List<string>
        {
            $"Periodo: {from:dd/MM/yyyy} al {to:dd/MM/yyyy}",
            $"Total ventas: {sales.Count}",
            $"Neto: {sales.Sum(item => item.NetTotal):C0} | Cobrado: {sales.Sum(item => item.PaidTotal):C0} | Saldo: {sales.Sum(item => item.BalanceDue):C0}",
            ""
        };
        lines.AddRange(sales.Select(item => $"#{item.SaleId:000000} {item.CreatedAt:dd/MM/yyyy HH:mm} | {item.Customer} | Neto {item.NetTotal:C0} | Cobrado {item.PaidTotal:C0} | Saldo {item.BalanceDue:C0} | {item.Status}"));
        SimplePdfWriter.Write(path, "Reporte de ventas", lines);
        return path;
    }

    public string ExportSalesReport(string directory, DateOnly from, DateOnly to, IReadOnlyList<SaleRow> sales)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"ventas-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx");
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Ventas");
        var headers = new[] { "Fecha", "Cliente", "Bruto", "Devuelto", "Neto", "Cobrado", "Saldo", "Estado" };
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
            worksheet.Cell(1, index + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var sale in sales)
        {
            worksheet.Cell(row, 1).Value = sale.CreatedAt.DateTime;
            worksheet.Cell(row, 2).Value = sale.Customer;
            worksheet.Cell(row, 3).Value = sale.GrossTotal;
            worksheet.Cell(row, 4).Value = sale.GrossTotal - sale.NetTotal;
            worksheet.Cell(row, 5).Value = sale.NetTotal;
            worksheet.Cell(row, 6).Value = sale.PaidTotal;
            worksheet.Cell(row, 7).Value = sale.BalanceDue;
            worksheet.Cell(row, 8).Value = sale.Status.ToString();
            row++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
        return path;
    }

    public string ExportInventoryPdfReport(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"inventario-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        var inventory = getInventory();
        var lines = new List<string>
        {
            $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}",
            $"Items: {inventory.Count}",
            $"Stock total: {inventory.Sum(item => item.StockQuantity)}",
            $"Valorizado costo: {inventory.Sum(item => item.StockQuantity * item.SupplierCost):C0}",
            $"Valorizado venta: {inventory.Sum(item => item.StockQuantity * item.CashPrice):C0}",
            ""
        };
        lines.AddRange(inventory.Select(item => $"{item.Brand} {item.Model} | {item.Color} {item.Size} | Stock {item.StockQuantity} | Costo {item.SupplierCost:C0} | Precio {item.CashPrice:C0}"));
        SimplePdfWriter.Write(path, "Reporte de inventario", lines);
        return path;
    }

    public string ExportInventoryReport(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"inventario-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Inventario");
        var headers = new[] { "Marca", "Modelo", "Color", "Talle", "Codigo", "Stock", "Costo proveedor", "Precio efectivo", "Valorizado costo", "Valorizado venta" };
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
            worksheet.Cell(1, index + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var item in getInventory())
        {
            worksheet.Cell(row, 1).Value = item.Brand;
            worksheet.Cell(row, 2).Value = item.Model;
            worksheet.Cell(row, 3).Value = item.Color;
            worksheet.Cell(row, 4).Value = item.Size;
            worksheet.Cell(row, 5).Value = item.Sku;
            worksheet.Cell(row, 6).Value = item.StockQuantity;
            worksheet.Cell(row, 7).Value = item.SupplierCost;
            worksheet.Cell(row, 8).Value = item.CashPrice;
            worksheet.Cell(row, 9).Value = item.StockQuantity * item.SupplierCost;
            worksheet.Cell(row, 10).Value = item.StockQuantity * item.CashPrice;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
        return path;
    }

    public string ExportStockMovementsReport(string directory, int? productId = null, int? variantId = null)
    {
        Directory.CreateDirectory(directory);
        var suffix = productId is null && variantId is null ? "ultimos" : "filtrado";
        var path = Path.Combine(directory, $"movimientos-stock-{suffix}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Movimientos");
        var headers = new[] { "Fecha", "Marca", "Modelo", "Color", "Talle", "Codigo", "Tipo", "Cantidad", "Stock final", "Motivo" };
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
            worksheet.Cell(1, index + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var item in getStockMovements(productId, variantId, 1000))
        {
            worksheet.Cell(row, 1).Value = item.OccurredAt.DateTime;
            worksheet.Cell(row, 2).Value = item.Brand;
            worksheet.Cell(row, 3).Value = item.Model;
            worksheet.Cell(row, 4).Value = item.Color;
            worksheet.Cell(row, 5).Value = item.Size;
            worksheet.Cell(row, 6).Value = item.Sku;
            worksheet.Cell(row, 7).Value = FormatStockMovementType(item.Type);
            worksheet.Cell(row, 8).Value = item.QuantityChange;
            worksheet.Cell(row, 9).Value = item.StockAfter;
            worksheet.Cell(row, 10).Value = item.Reason;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
        return path;
    }

    private static decimal CalculateMargin(decimal netSales, decimal grossProfit)
    {
        return netSales == 0m
            ? 0m
            : decimal.Round(grossProfit / netSales * 100m, 2, MidpointRounding.AwayFromZero);
    }

    private static string FormatStockMovementType(StockMovementType type)
    {
        return type switch
        {
            StockMovementType.Purchase => "Compra",
            StockMovementType.Sale => "Venta",
            StockMovementType.Return => "Devolucion",
            StockMovementType.Adjustment => "Ajuste",
            StockMovementType.Import => "Importacion",
            _ => type.ToString()
        };
    }
}
