using System.Windows.Controls;

namespace FPCalzados.App;

public partial class MainWindow
{
    private DataGrid SuppliersGrid => SuppliersViewControl.SuppliersGrid;
    private TextBox SupplierIdBox => SuppliersViewControl.SupplierIdBox;
    private TextBox SupplierNameBox => SuppliersViewControl.SupplierNameBox;
    private TextBox SupplierBrandBox => SuppliersViewControl.SupplierBrandBox;

    private void WireSupplierViewEvents()
    {
        SuppliersViewControl.RefreshSuppliersButton.Click += RefreshSuppliers_Click;
        SuppliersViewControl.NewSupplierButton.Click += NewSupplier_Click;
        SuppliersViewControl.DeactivateSupplierButton.Click += DeactivateSupplier_Click;
        SuppliersViewControl.SaveSupplierButton.Click += SaveSupplier_Click;
        SuppliersViewControl.SuppliersGrid.SelectionChanged += SuppliersGrid_SelectionChanged;
    }
}
