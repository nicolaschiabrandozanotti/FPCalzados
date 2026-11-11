using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FPCalzados.App.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
    }

    public event SelectionChangedEventHandler? SalesTabsSelectionChanged;
    public event RoutedEventHandler? RefreshSaleInventoryRequested;
    public event RoutedEventHandler? QuickAddSaleRequested;
    public event SelectionChangedEventHandler? SaleVariantFilterSelectionChanged;
    public event TextChangedEventHandler? SalePriceInputChanged;
    public event SelectionChangedEventHandler? SaleRuleSelectionChanged;
    public event RoutedEventHandler? AddCartRequested;
    public event SelectionChangedEventHandler? SaleProductGridSelectionChanged;
    public event MouseButtonEventHandler? SaleProductGridPreviewMouseLeftButtonDownRequested;
    public event MouseButtonEventHandler? SaleProductGridMouseDoubleClickRequested;
    public event SelectionChangedEventHandler? SaleOptionGridSelectionChanged;
    public event MouseButtonEventHandler? SaleOptionGridMouseDoubleClickRequested;
    public event KeyEventHandler? SearchBoxKeyDown;
    public event RoutedEventHandler? RemoveCartRequested;
    public event RoutedEventHandler? AddPaymentRequested;
    public event RoutedEventHandler? RemovePaymentRequested;
    public event RoutedEventHandler? SaveSaleRequested;
    public event RoutedEventHandler? ClearSaleRequested;
    public event RoutedEventHandler? RefreshSalesRequested;
    public event RoutedEventHandler? PreviousSalesPageRequested;
    public event RoutedEventHandler? NextSalesPageRequested;
    public event RoutedEventHandler? ViewSaleDetailRequested;
    public event RoutedEventHandler? PrintSaleReceiptRequested;
    public event RoutedEventHandler? ExportSaleReceiptRequested;
    public event RoutedEventHandler? ExportSaleDetailRequested;
    public event RoutedEventHandler? ChangeDueDateRequested;
    public event RoutedEventHandler? RegisterPaymentRequested;
    public event RoutedEventHandler? ReturnSelectedSaleRequested;
    public event RoutedEventHandler? RefreshCustomerDebtsRequested;
    public event RoutedEventHandler? ViewCustomerHistoryRequested;
    public event MouseButtonEventHandler? CustomerDebtsGridMouseDoubleClickRequested;
    public event RoutedEventHandler? ExportCustomerDebtsRequested;
    public event RoutedEventHandler? RefreshPaymentCollectionsRequested;
    public event RoutedEventHandler? ExportPaymentCollectionsRequested;

    private void SalesTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) => SalesTabsSelectionChanged?.Invoke(sender, e);
    private void RefreshSaleInventory_Click(object sender, RoutedEventArgs e) => RefreshSaleInventoryRequested?.Invoke(sender, e);
    private void QuickAddSale_Click(object sender, RoutedEventArgs e) => QuickAddSaleRequested?.Invoke(sender, e);
    private void SaleVariantFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => SaleVariantFilterSelectionChanged?.Invoke(sender, e);
    private void SalePriceInput_Changed(object sender, TextChangedEventArgs e) => SalePriceInputChanged?.Invoke(sender, e);
    private void SaleRuleBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => SaleRuleSelectionChanged?.Invoke(sender, e);
    private void AddCart_Click(object sender, RoutedEventArgs e) => AddCartRequested?.Invoke(sender, e);
    private void SaleProductGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => SaleProductGridSelectionChanged?.Invoke(sender, e);
    private void SaleProductGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => SaleProductGridPreviewMouseLeftButtonDownRequested?.Invoke(sender, e);
    private void SaleProductGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SaleProductGridMouseDoubleClickRequested?.Invoke(sender, e);
    private void SaleOptionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => SaleOptionGridSelectionChanged?.Invoke(sender, e);
    private void SaleOptionGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SaleOptionGridMouseDoubleClickRequested?.Invoke(sender, e);
    private void SearchBox_KeyDown(object sender, KeyEventArgs e) => SearchBoxKeyDown?.Invoke(sender, e);
    private void RemoveCart_Click(object sender, RoutedEventArgs e) => RemoveCartRequested?.Invoke(sender, e);
    private void AddPayment_Click(object sender, RoutedEventArgs e) => AddPaymentRequested?.Invoke(sender, e);
    private void RemovePayment_Click(object sender, RoutedEventArgs e) => RemovePaymentRequested?.Invoke(sender, e);
    private void SaveSale_Click(object sender, RoutedEventArgs e) => SaveSaleRequested?.Invoke(sender, e);
    private void ClearSale_Click(object sender, RoutedEventArgs e) => ClearSaleRequested?.Invoke(sender, e);
    private void RefreshSales_Click(object sender, RoutedEventArgs e) => RefreshSalesRequested?.Invoke(sender, e);
    private void PreviousSalesPage_Click(object sender, RoutedEventArgs e) => PreviousSalesPageRequested?.Invoke(sender, e);
    private void NextSalesPage_Click(object sender, RoutedEventArgs e) => NextSalesPageRequested?.Invoke(sender, e);
    private void ViewSaleDetail_Click(object sender, RoutedEventArgs e) => ViewSaleDetailRequested?.Invoke(sender, e);
    private void PrintSaleReceipt_Click(object sender, RoutedEventArgs e) => PrintSaleReceiptRequested?.Invoke(sender, e);
    private void ExportSaleReceipt_Click(object sender, RoutedEventArgs e) => ExportSaleReceiptRequested?.Invoke(sender, e);
    private void ExportSaleDetail_Click(object sender, RoutedEventArgs e) => ExportSaleDetailRequested?.Invoke(sender, e);
    private void ChangeDueDate_Click(object sender, RoutedEventArgs e) => ChangeDueDateRequested?.Invoke(sender, e);
    private void RegisterPayment_Click(object sender, RoutedEventArgs e) => RegisterPaymentRequested?.Invoke(sender, e);
    private void ReturnSelectedSale_Click(object sender, RoutedEventArgs e) => ReturnSelectedSaleRequested?.Invoke(sender, e);
    private void RefreshCustomerDebts_Click(object sender, RoutedEventArgs e) => RefreshCustomerDebtsRequested?.Invoke(sender, e);
    private void ViewCustomerHistory_Click(object sender, RoutedEventArgs e) => ViewCustomerHistoryRequested?.Invoke(sender, e);
    private void CustomerDebtsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => CustomerDebtsGridMouseDoubleClickRequested?.Invoke(sender, e);
    private void ExportCustomerDebts_Click(object sender, RoutedEventArgs e) => ExportCustomerDebtsRequested?.Invoke(sender, e);
    private void RefreshPaymentCollections_Click(object sender, RoutedEventArgs e) => RefreshPaymentCollectionsRequested?.Invoke(sender, e);
    private void ExportPaymentCollections_Click(object sender, RoutedEventArgs e) => ExportPaymentCollectionsRequested?.Invoke(sender, e);
}
