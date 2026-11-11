using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using FPCalzados.Infrastructure;

namespace FPCalzados.App;

public partial class MainWindow
{
    private async void RefreshSuppliers_Click(object sender, RoutedEventArgs e) => await RefreshSuppliersAsync();

    private async Task RefreshSuppliersAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _suppliersViewModel.LoadAsync();
            ApplySuppliers(_suppliersViewModel.Suppliers);
        }, "Actualizando proveedores...");
    }

    private void RefreshSuppliers()
    {
        _suppliersViewModel.Load();
        ApplySuppliers(_suppliersViewModel.Suppliers);
    }

    private void ApplySuppliers(IReadOnlyList<SupplierRow> suppliers)
    {
        _suppliers = suppliers;
        SuppliersGrid.ItemsSource = _suppliers;
        ProductSupplierBox.ItemsSource = _suppliers;
    }

    private void NewSupplier_Click(object sender, RoutedEventArgs e)
    {
        SupplierIdBox.Text = "";
        SupplierNameBox.Text = "";
        SupplierBrandBox.Text = "";
        SuppliersGrid.SelectedItem = null;
    }

    private void SuppliersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SuppliersGrid.SelectedItem is not SupplierRow row)
        {
            return;
        }

        SupplierIdBox.Text = row.Id.ToString(CultureInfo.InvariantCulture);
        SupplierNameBox.Text = row.Name;
        SupplierBrandBox.Text = row.Brand;
    }

    private async void SaveSupplier_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            var request = new SupplierEditRequest(
                ParseNullableInt(SupplierIdBox.Text),
                SupplierNameBox.Text,
                SupplierBrandBox.Text,
                true);

            await RunBusyAsync(async () =>
            {
                await _suppliersViewModel.SaveSupplierAsync(request);
                RefreshSuppliers();
                _saleInventoryLoaded = false;
                Status("Proveedor guardado.");
            }, "Guardando proveedor...");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void DeactivateSupplier_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            if (SuppliersGrid.SelectedItem is not SupplierRow row)
            {
                throw new InvalidOperationException("Selecciona un proveedor.");
            }

            await RunBusyAsync(async () =>
            {
                await _suppliersViewModel.DeactivateSupplierAsync(row.Id);
                RefreshSuppliers();
                _saleInventoryLoaded = false;
                Status("Proveedor dado de baja.");
            }, "Dando de baja proveedor...");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

}
