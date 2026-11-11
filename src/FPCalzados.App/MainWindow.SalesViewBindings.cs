using System.Windows.Controls;

namespace FPCalzados.App;

public partial class MainWindow
{
    private TabControl SalesTabs => SalesViewControl.SalesTabs;
    private TextBox SaleSearchBox => SalesViewControl.SaleSearchBox;
    private ComboBox SaleSizeFilterBox => SalesViewControl.SaleSizeFilterBox;
    private ComboBox SaleColorFilterBox => SalesViewControl.SaleColorFilterBox;
    private TextBox SaleQuantityBox => SalesViewControl.SaleQuantityBox;
    private TextBox SaleDiscountBox => SalesViewControl.SaleDiscountBox;
    private ComboBox SaleRuleBox => SalesViewControl.SaleRuleBox;
    private TextBlock SaleFinalPricePreviewText => SalesViewControl.SaleFinalPricePreviewText;
    private DataGrid SaleProductGrid => SalesViewControl.SaleProductGrid;
    private TextBlock SelectedSaleVariantText => SalesViewControl.SelectedSaleVariantText;
    private DataGrid CartGrid => SalesViewControl.CartGrid;
    private TextBlock SaleTotalText => SalesViewControl.SaleTotalText;
    private TextBlock SaleCartBreakdownText => SalesViewControl.SaleCartBreakdownText;
    private ComboBox PaymentMethodBox => SalesViewControl.PaymentMethodBox;
    private TextBox PaymentAmountBox => SalesViewControl.PaymentAmountBox;
    private TextBox PaymentInstallmentsBox => SalesViewControl.PaymentInstallmentsBox;
    private DataGrid PaymentsGrid => SalesViewControl.PaymentsGrid;
    private TextBox CustomerFirstNameBox => SalesViewControl.CustomerFirstNameBox;
    private TextBox CustomerLastNameBox => SalesViewControl.CustomerLastNameBox;
    private TextBox CustomerPhoneBox => SalesViewControl.CustomerPhoneBox;
    private DatePicker SaleDueDatePicker => SalesViewControl.SaleDueDatePicker;
    private TextBox SalesSearchBox => SalesViewControl.SalesSearchBox;
    private DataGrid SalesGrid => SalesViewControl.SalesGrid;
    private TextBlock SalesPageText => SalesViewControl.SalesPageText;
    private DataGrid CustomerDebtsGrid => SalesViewControl.CustomerDebtsGrid;
    private DatePicker CollectionsFromPicker => SalesViewControl.CollectionsFromPicker;
    private DatePicker CollectionsToPicker => SalesViewControl.CollectionsToPicker;
    private DataGrid PaymentCollectionsGrid => SalesViewControl.PaymentCollectionsGrid;

    private void WireSalesViewEvents()
    {
        SalesViewControl.SalesTabsSelectionChanged += SalesTabs_SelectionChanged;
        SalesViewControl.RefreshSaleInventoryRequested += RefreshSaleInventory_Click;
        SalesViewControl.QuickAddSaleRequested += QuickAddSale_Click;
        SalesViewControl.SaleVariantFilterSelectionChanged += SaleVariantFilter_SelectionChanged;
        SalesViewControl.SalePriceInputChanged += SalePriceInput_Changed;
        SalesViewControl.SaleRuleSelectionChanged += SaleRuleBox_SelectionChanged;
        SalesViewControl.AddCartRequested += AddCart_Click;
        SalesViewControl.SaleProductGridSelectionChanged += SaleProductGrid_SelectionChanged;
        SalesViewControl.SaleProductGridPreviewMouseLeftButtonDownRequested += SaleProductGrid_PreviewMouseLeftButtonDown;
        SalesViewControl.SaleProductGridMouseDoubleClickRequested += SaleProductGrid_MouseDoubleClick;
        SalesViewControl.SaleOptionGridSelectionChanged += SaleOptionGrid_SelectionChanged;
        SalesViewControl.SaleOptionGridMouseDoubleClickRequested += SaleOptionGrid_MouseDoubleClick;
        SalesViewControl.SearchBoxKeyDown += SearchBox_KeyDown;
        SalesViewControl.RemoveCartRequested += RemoveCart_Click;
        SalesViewControl.AddPaymentRequested += AddPayment_Click;
        SalesViewControl.RemovePaymentRequested += RemovePayment_Click;
        SalesViewControl.SaveSaleRequested += SaveSale_Click;
        SalesViewControl.ClearSaleRequested += ClearSale_Click;
        SalesViewControl.RefreshSalesRequested += RefreshSales_Click;
        SalesViewControl.PreviousSalesPageRequested += PreviousSalesPage_Click;
        SalesViewControl.NextSalesPageRequested += NextSalesPage_Click;
        SalesViewControl.ViewSaleDetailRequested += ViewSaleDetail_Click;
        SalesViewControl.PrintSaleReceiptRequested += PrintSaleReceipt_Click;
        SalesViewControl.ExportSaleReceiptRequested += ExportSaleReceipt_Click;
        SalesViewControl.ExportSaleDetailRequested += ExportSaleDetail_Click;
        SalesViewControl.ChangeDueDateRequested += ChangeDueDate_Click;
        SalesViewControl.RegisterPaymentRequested += RegisterPayment_Click;
        SalesViewControl.ReturnSelectedSaleRequested += ReturnSelectedSale_Click;
        SalesViewControl.RefreshCustomerDebtsRequested += RefreshCustomerDebts_Click;
        SalesViewControl.ViewCustomerHistoryRequested += ViewCustomerHistory_Click;
        SalesViewControl.CustomerDebtsGridMouseDoubleClickRequested += CustomerDebtsGrid_MouseDoubleClick;
        SalesViewControl.ExportCustomerDebtsRequested += ExportCustomerDebts_Click;
        SalesViewControl.RefreshPaymentCollectionsRequested += RefreshPaymentCollections_Click;
        SalesViewControl.ExportPaymentCollectionsRequested += ExportPaymentCollections_Click;
    }
}
