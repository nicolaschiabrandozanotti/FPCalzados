using System.Windows.Controls;

namespace FPCalzados.App;

public partial class MainWindow
{
    private DatePicker ReportFromPicker => ReportsViewControl.ReportFromPicker;
    private DatePicker ReportToPicker => ReportsViewControl.ReportToPicker;
    private TextBox ReportLogBox => ReportsViewControl.ReportLogBox;

    private void WireReportsViewEvents()
    {
        ReportsViewControl.CreateStockTemplateButton.Click += CreateStockTemplate_Click;
        ReportsViewControl.ImportStockButton.Click += ImportStock_Click;
        ReportsViewControl.ExportSalesButton.Click += ExportSales_Click;
        ReportsViewControl.ExportSalesPdfButton.Click += ExportSalesPdf_Click;
        ReportsViewControl.ExportInventoryButton.Click += ExportInventory_Click;
        ReportsViewControl.ExportInventoryPdfButton.Click += ExportInventoryPdf_Click;
        ReportsViewControl.ExportProfitabilityButton.Click += ExportProfitability_Click;
    }
}
