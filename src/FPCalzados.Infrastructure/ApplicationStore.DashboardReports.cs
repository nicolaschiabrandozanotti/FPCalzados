using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

public sealed partial class ApplicationStore
{
    public DashboardSummary GetDashboard(DateOnly from, DateOnly to)
    {
        return dashboardReports.GetDashboard(from, to);
    }

    public CashRegisterSummary GetCashRegister(DateOnly from, DateOnly to)
    {
        return dashboardReports.GetCashRegister(from, to);
    }

    public string ExportCashRegisterReport(string directory, DateOnly from, DateOnly to)
    {
        return dashboardReports.ExportCashRegisterReport(directory, from, to);
    }

    public CashRegisterSession OpenCashRegister(decimal openingCash, string? notes)
    {
        return dashboardReports.OpenCashRegister(openingCash, notes, currentRole);
    }

    public CashRegisterSession CloseCashRegister(decimal closingCash, string? notes)
    {
        return dashboardReports.CloseCashRegister(closingCash, notes, currentRole);
    }

    public ProfitabilitySummary GetProfitability(DateOnly from, DateOnly to)
    {
        return dashboardReports.GetProfitability(from, to);
    }

    public string ExportProfitabilityReport(string directory, DateOnly from, DateOnly to)
    {
        return dashboardReports.ExportProfitabilityReport(directory, from, to);
    }

    public string ExportSalesReport(string directory, DateOnly from, DateOnly to)
    {
        return dashboardReports.ExportSalesReport(directory, from, to, GetSales(from, to));
    }

    public string ExportSalesPdfReport(string directory, DateOnly from, DateOnly to)
    {
        return dashboardReports.ExportSalesPdfReport(directory, from, to, GetSales(from, to));
    }

    public string ExportInventoryReport(string directory)
    {
        return dashboardReports.ExportInventoryReport(directory);
    }

    public string ExportInventoryPdfReport(string directory)
    {
        return dashboardReports.ExportInventoryPdfReport(directory);
    }

    public string ExportStockMovementsReport(string directory, int? productId = null, int? variantId = null)
    {
        return dashboardReports.ExportStockMovementsReport(directory, productId, variantId);
    }
}
