using System.Windows.Controls;

namespace FPCalzados.App;

public partial class MainWindow
{
    private DatePicker DashboardFromPicker => DashboardViewControl.DashboardFromPicker;
    private DatePicker DashboardToPicker => DashboardViewControl.DashboardToPicker;
    private TextBlock GrossSalesText => DashboardViewControl.GrossSalesText;
    private TextBlock NetSalesText => DashboardViewControl.NetSalesText;
    private TextBlock PaidSalesText => DashboardViewControl.PaidSalesText;
    private TextBlock PendingSalesText => DashboardViewControl.PendingSalesText;
    private TextBlock ReturnedSalesText => DashboardViewControl.ReturnedSalesText;
    private TextBlock LowStockText => DashboardViewControl.LowStockText;
    private TextBlock OverdueText => DashboardViewControl.OverdueText;
    private DataGrid TopProductsGrid => DashboardViewControl.TopProductsGrid;
    private DataGrid AlertsGrid => DashboardViewControl.AlertsGrid;
    private TextBlock CashRegisterStatusText => DashboardViewControl.CashRegisterStatusText;
    private TextBlock CashRegisterSalesCountText => DashboardViewControl.CashRegisterSalesCountText;
    private TextBlock CashRegisterCashText => DashboardViewControl.CashRegisterCashText;
    private TextBlock CashRegisterDebitText => DashboardViewControl.CashRegisterDebitText;
    private TextBlock CashRegisterCreditText => DashboardViewControl.CashRegisterCreditText;
    private TextBlock CashRegisterPaidText => DashboardViewControl.CashRegisterPaidText;
    private TextBlock CashRegisterPendingText => DashboardViewControl.CashRegisterPendingText;
    private TextBlock CashRegisterReturnedText => DashboardViewControl.CashRegisterReturnedText;
    private TextBlock CashRegisterCashToReturnText => DashboardViewControl.CashRegisterCashToReturnText;
    private TextBlock CashRegisterOpeningCashText => DashboardViewControl.CashRegisterOpeningCashText;
    private TextBlock CashRegisterExpectedCashText => DashboardViewControl.CashRegisterExpectedCashText;
    private TextBlock CashRegisterClosingCashText => DashboardViewControl.CashRegisterClosingCashText;
    private TextBlock CashRegisterDifferenceText => DashboardViewControl.CashRegisterDifferenceText;

    private void WireDashboardViewEvents()
    {
        DashboardViewControl.RefreshDashboardButton.Click += RefreshDashboard_Click;
        DashboardViewControl.OpenCashRegisterButton.Click += OpenCashRegister_Click;
        DashboardViewControl.CloseCashRegisterButton.Click += CloseCashRegister_Click;
        DashboardViewControl.ExportCashRegisterButton.Click += ExportCashRegister_Click;
    }
}
