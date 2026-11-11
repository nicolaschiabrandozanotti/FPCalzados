using System.Windows.Controls;

namespace FPCalzados.App;

public partial class MainWindow
{
    private TextBox InventorySearchBox => InventoryViewControl.InventorySearchBox;
    private DataGrid InventoryGrid => InventoryViewControl.InventoryGrid;
    private TextBlock StockMovementFilterText => InventoryViewControl.StockMovementFilterText;
    private DataGrid StockMovementsGrid => InventoryViewControl.StockMovementsGrid;
    private TextBox ProductIdBox => InventoryViewControl.ProductIdBox;
    private ComboBox ProductSupplierBox => InventoryViewControl.ProductSupplierBox;
    private TextBox BrandBox => InventoryViewControl.BrandBox;
    private TextBox ModelBox => InventoryViewControl.ModelBox;
    private TextBox VariantColorBox => InventoryViewControl.VariantColorBox;
    private TextBox VariantSizeBox => InventoryViewControl.VariantSizeBox;
    private TextBox VariantStockBox => InventoryViewControl.VariantStockBox;
    private DataGrid VariantEditGrid => InventoryViewControl.VariantEditGrid;
    private TextBlock TotalStockText => InventoryViewControl.TotalStockText;
    private TextBox SupplierCostBox => InventoryViewControl.SupplierCostBox;
    private TextBox CashPriceBox => InventoryViewControl.CashPriceBox;
    private TextBox LowStockBox => InventoryViewControl.LowStockBox;

    private void WireInventoryViewEvents()
    {
        InventoryViewControl.InventorySearchBox.KeyDown += SearchBox_KeyDown;
        InventoryViewControl.RefreshInventoryButton.Click += RefreshInventory_Click;
        InventoryViewControl.NewProductButton.Click += NewProduct_Click;
        InventoryViewControl.AdjustStockButton.Click += AdjustStock_Click;
        InventoryViewControl.ShowLowStockButton.Click += ShowLowStock_Click;
        InventoryViewControl.DeactivateButton.Click += DeactivateVariant_Click;
        InventoryViewControl.InventoryGrid.SelectionChanged += InventoryGrid_SelectionChanged;
        InventoryViewControl.InventoryGrid.MouseDoubleClick += InventoryGrid_MouseDoubleClick;
        InventoryViewControl.RefreshStockMovementsButton.Click += RefreshStockMovements_Click;
        InventoryViewControl.RefreshSelectedStockMovementsButton.Click += RefreshSelectedStockMovements_Click;
        InventoryViewControl.ExportStockMovementsButton.Click += ExportStockMovements_Click;
        InventoryViewControl.ExportSelectedStockMovementsButton.Click += ExportSelectedStockMovements_Click;
        InventoryViewControl.ProductSupplierBox.SelectionChanged += ProductSupplierBox_SelectionChanged;
        InventoryViewControl.AddVariantRowButton.Click += AddVariantRow_Click;
        InventoryViewControl.RemoveSelectedVariantRowButton.Click += RemoveSelectedVariantRow_Click;
        InventoryViewControl.VariantEditGrid.MouseDoubleClick += VariantEditGrid_MouseDoubleClick;
        InventoryViewControl.SaveProductButton.Click += SaveProduct_Click;
    }
}
