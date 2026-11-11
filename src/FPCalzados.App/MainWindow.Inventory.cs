using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FPCalzados.Domain;
using FPCalzados.Infrastructure;
using Microsoft.Win32;

namespace FPCalzados.App;

public partial class MainWindow
{
    private async void RefreshInventory_Click(object sender, RoutedEventArgs e) => await RefreshInventoryAsync();

    private async Task RefreshInventoryAsync()
    {
        _inventoryViewModel.SearchText = InventorySearchBox.Text;
        await RunBusyAsync(async () =>
        {
            await _inventoryViewModel.LoadInventoryAsync();
            ApplyInventoryRows(_inventoryViewModel.Rows);
            ApplyStockMovements(_inventoryViewModel.StockMovements, _inventoryViewModel.StockMovementFilterText);
        }, "Actualizando inventario...");
    }

    private void RefreshInventory()
    {
        _inventoryViewModel.SearchText = InventorySearchBox.Text;
        _inventoryViewModel.LoadInventory();
        ApplyInventoryRows(_inventoryViewModel.Rows);
        ApplyStockMovements(_inventoryViewModel.StockMovements, _inventoryViewModel.StockMovementFilterText);
    }

    private void ApplyInventoryRows(IReadOnlyList<InventoryRow> rows)
    {
        InventoryGrid.ItemsSource = rows;
        if (string.IsNullOrWhiteSpace(SaleSearchBox.Text))
        {
            _saleInventoryLoaded = false;
        }
    }

    private async void RefreshStockMovements_Click(object sender, RoutedEventArgs e) => await RefreshStockMovementsAsync();

    private async void RefreshSelectedStockMovements_Click(object sender, RoutedEventArgs e) => await RefreshSelectedStockMovementsAsync();

    private async void ExportStockMovements_Click(object sender, RoutedEventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            var path = await _inventoryViewModel.ExportStockMovementsReportAsync(_paths.ExportDirectory);
            Log($"Reporte de movimientos de stock: {path}");
        }, "Exportando movimientos de stock...");
    }

    private async void ExportSelectedStockMovements_Click(object sender, RoutedEventArgs e)
    {
        if (InventoryGrid.SelectedItem is not InventoryRow row)
        {
            ShowError(new InvalidOperationException("Selecciona un producto para exportar su historial."));
            return;
        }

        await RunBusyAsync(async () =>
        {
            var path = await _inventoryViewModel.ExportStockMovementsReportAsync(_paths.ExportDirectory, row.ProductId);
            Log($"Reporte de movimientos de {row.Brand} {row.Model}: {path}");
        }, "Exportando historial de stock...");
    }

    private async Task RefreshStockMovementsAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _inventoryViewModel.LoadStockMovementsAsync();
            ApplyStockMovements(_inventoryViewModel.StockMovements, _inventoryViewModel.StockMovementFilterText);
        }, "Actualizando movimientos...");
    }

    private void RefreshStockMovements()
    {
        _inventoryViewModel.LoadStockMovements();
        ApplyStockMovements(_inventoryViewModel.StockMovements, _inventoryViewModel.StockMovementFilterText);
    }

    private async Task RefreshSelectedStockMovementsAsync()
    {
        var selected = InventoryGrid.SelectedItem as InventoryRow;
        await RunBusyAsync(async () =>
        {
            await _inventoryViewModel.LoadStockMovementsForProductAsync(selected);
            ApplyStockMovements(_inventoryViewModel.StockMovements, _inventoryViewModel.StockMovementFilterText);
        }, "Actualizando historial...");
    }

    private void RefreshSelectedStockMovements()
    {
        _inventoryViewModel.LoadStockMovementsForProduct(InventoryGrid.SelectedItem as InventoryRow);
        ApplyStockMovements(_inventoryViewModel.StockMovements, _inventoryViewModel.StockMovementFilterText);
    }

    private void ApplyStockMovements(IReadOnlyList<StockMovementRow> rows, string filterText)
    {
        StockMovementsGrid.ItemsSource = rows;
        StockMovementFilterText.Text = filterText;
    }


    private void InventoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InventoryGrid.SelectedItem is not InventoryRow row)
        {
            return;
        }

        LoadProductForEditing(row);
        RefreshSelectedStockMovements();
    }

    private void InventoryGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (InventoryGrid.SelectedItem is not InventoryRow row)
        {
            return;
        }

        LoadProductForEditing(row);
        var columnHeader = GetDataGridColumnHeader(e.OriginalSource as DependencyObject);
        FocusProductEditor(row, columnHeader);
        e.Handled = true;
    }

    private void FocusProductEditor(InventoryRow row, string? columnHeader)
    {
        if (string.Equals(columnHeader, "Marca", StringComparison.OrdinalIgnoreCase))
        {
            BrandBox.Focus();
            BrandBox.SelectAll();
            return;
        }

        if (string.Equals(columnHeader, "Modelo", StringComparison.OrdinalIgnoreCase))
        {
            ModelBox.Focus();
            ModelBox.SelectAll();
            return;
        }

        if (string.Equals(columnHeader, "Costo", StringComparison.OrdinalIgnoreCase))
        {
            SupplierCostBox.Focus();
            SupplierCostBox.SelectAll();
            return;
        }

        if (string.Equals(columnHeader, "Precio efectivo", StringComparison.OrdinalIgnoreCase))
        {
            CashPriceBox.Focus();
            CashPriceBox.SelectAll();
            return;
        }

        var editRow = _productVariantEdits.FirstOrDefault(item => item.VariantId == row.VariantId);
        if (editRow is null)
        {
            return;
        }

        VariantEditGrid.SelectedItem = editRow;
        VariantEditGrid.Focus();
        var columnIndex = columnHeader switch
        {
            "Color" => 0,
            "Talle" => 1,
            "Stock" => 2,
            _ => 0
        };
        BeginVariantCellEdit(editRow, columnIndex);
    }

    private void LoadProductForEditing(InventoryRow row)
    {
        ProductIdBox.Text = row.ProductId.ToString(CultureInfo.InvariantCulture);
        ProductSupplierBox.SelectedItem = _suppliers.FirstOrDefault(item =>
            string.Equals(item.Brand, row.Brand, StringComparison.OrdinalIgnoreCase));
        BrandBox.Text = row.Brand;
        ModelBox.Text = row.Model;
        LowStockBox.Text = row.LowStockThreshold.ToString(CultureInfo.InvariantCulture);
        SupplierCostBox.Text = row.SupplierCost == 0m ? "" : row.SupplierCost.ToString(CultureInfo.InvariantCulture);
        CashPriceBox.Text = row.CashPrice.ToString(CultureInfo.InvariantCulture);
        ClearVariantInputs();
        _productVariantEdits.Clear();
        foreach (var option in _inventoryViewModel.GetProductOptions(row.ProductId))
        {
            AddVariantRow(option);
        }

        UpdateTotalStock();
    }

    private void NewProduct_Click(object sender, RoutedEventArgs e)
    {
        ProductIdBox.Text = "";
        ProductSupplierBox.SelectedItem = null;
        BrandBox.Text = "";
        ModelBox.Text = "";
        LowStockBox.Text = "1";
        SupplierCostBox.Text = "";
        CashPriceBox.Text = "0";
        _productVariantEdits.Clear();
        ClearVariantInputs();
        InventoryGrid.SelectedItem = null;
        Status("Carga marca, modelo, las filas de color/talle/stock y el precio de venta.");
    }

    private void ProductSupplierBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductSupplierBox.SelectedItem is SupplierRow row)
        {
            BrandBox.Text = row.Brand;
        }
    }

    private async void SaveProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            AddPendingVariantIfNeeded();
            var variants = _productVariantEdits
                .Where(item => item.VariantId is not null ||
                               !string.IsNullOrWhiteSpace(item.Color) ||
                               !string.IsNullOrWhiteSpace(item.Size) ||
                               item.StockQuantity != 0)
                .Select(item => new ProductVariantStockEditRequest(
                    item.VariantId,
                    item.Color,
                    item.Size,
                    item.StockQuantity,
                    item.Sku))
                .ToList();

            var request = new ProductStockEditRequest(
                ParseNullableInt(ProductIdBox.Text),
                BrandBox.Text,
                ModelBox.Text,
                ParseInt(LowStockBox.Text, "alerta bajo stock"),
                ParseOptionalDecimal(SupplierCostBox.Text),
                ParseDecimal(CashPriceBox.Text, "precio efectivo"),
                variants);

            await RunBusyAsync(async () =>
            {
                await _inventoryViewModel.SaveProductStockAsync(request);
                RefreshAfterMutation();
                Status("Producto guardado.");
            }, "Guardando producto...");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void DeactivateVariant_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            if (InventoryGrid.SelectedItem is not InventoryRow row)
            {
                throw new InvalidOperationException("Selecciona una variante.");
            }

            await RunBusyAsync(async () =>
            {
                await _inventoryViewModel.DeactivateVariantAsync(row.VariantId);
                RefreshAfterMutation();
                Status("Variante dada de baja.");
            }, "Dando de baja variante...");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void AdjustStock_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            if (InventoryGrid.SelectedItem is not InventoryRow row)
            {
                throw new InvalidOperationException("Selecciona una variante para mover stock.");
            }

            var request = StockAdjustmentDialog.Show(this, row);
            if (request is null)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                await _inventoryViewModel.AdjustVariantStockAsync(row.VariantId, request.Value.QuantityChange, request.Value.Type, request.Value.Reason);
                RefreshAfterMutation();
                Status($"Stock actualizado. Movimiento: {request.Value.QuantityChange}.");
            }, "Actualizando stock...");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void ShowLowStock_Click(object sender, RoutedEventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            await _inventoryViewModel.LoadLowStockAsync();
            ApplyInventoryRows(_inventoryViewModel.Rows);
            Status($"Productos bajo stock: {_inventoryViewModel.Rows.Count}.");
        }, "Buscando bajo stock...");
    }

    private void AddVariantRow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AddVariantRowFromInputs();
            ClearVariantInputs();
            VariantColorBox.Focus();
            Status("Color, talle y stock agregados.");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void RemoveSelectedVariantRow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selected = GetSelectedVariantEditRow();
            if (selected is null)
            {
                throw new InvalidOperationException("Selecciona una opcion de color, talle y stock para eliminar.");
            }

            _productVariantEdits.Remove(selected);
            Status("Opcion de color, talle y stock eliminada.");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void VariantEditGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            grid.Focus();
            grid.BeginEdit(e);
        }
    }

    private ProductVariantEditRow? GetSelectedVariantEditRow()
    {
        if (VariantEditGrid.SelectedItem is ProductVariantEditRow selected)
        {
            return selected;
        }

        if (VariantEditGrid.CurrentCell.Item is ProductVariantEditRow current)
        {
            return current;
        }

        return VariantEditGrid.SelectedCells
            .Select(cell => cell.Item)
            .OfType<ProductVariantEditRow>()
            .FirstOrDefault();
    }

    private void BeginVariantCellEdit(ProductVariantEditRow row, int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= VariantEditGrid.Columns.Count)
        {
            columnIndex = 0;
        }

        VariantEditGrid.CurrentCell = new DataGridCellInfo(row, VariantEditGrid.Columns[columnIndex]);
        VariantEditGrid.ScrollIntoView(row, VariantEditGrid.Columns[columnIndex]);
        VariantEditGrid.BeginEdit();
    }

    private void AddVariantRow(InventoryRow? row = null)
    {
        _productVariantEdits.Add(new ProductVariantEditRow(
            row?.VariantId,
            row?.Color ?? "",
            row?.Size ?? "",
            row?.StockQuantity ?? 0,
            row?.Sku));
    }

    private void AddVariantRowFromInputs()
    {
        _productVariantEdits.Add(new ProductVariantEditRow(
            null,
            VariantColorBox.Text,
            VariantSizeBox.Text,
            ParseInt(VariantStockBox.Text, "stock"),
            null));
    }

    private void AddPendingVariantIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(VariantColorBox.Text) &&
            string.IsNullOrWhiteSpace(VariantSizeBox.Text) &&
            (string.IsNullOrWhiteSpace(VariantStockBox.Text) || VariantStockBox.Text.Trim() == "0"))
        {
            return;
        }

        AddVariantRowFromInputs();
        ClearVariantInputs();
    }

    private void ClearVariantInputs()
    {
        VariantColorBox.Text = "";
        VariantSizeBox.Text = "";
        VariantStockBox.Text = "0";
    }

    private void ProductVariantEdits_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ProductVariantEditRow row in e.OldItems)
            {
                row.PropertyChanged -= ProductVariantEdit_PropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (ProductVariantEditRow row in e.NewItems)
            {
                row.PropertyChanged += ProductVariantEdit_PropertyChanged;
            }
        }

        UpdateTotalStock();
    }

    private void ProductVariantEdit_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductVariantEditRow.StockQuantity))
        {
            UpdateTotalStock();
        }
    }

    private void UpdateTotalStock()
    {
        TotalStockText.Text = _productVariantEdits
            .Sum(item => item.StockQuantity)
            .ToString(CultureInfo.CurrentCulture);
    }

    private static string? GetDataGridColumnHeader(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is DataGridCell cell)
            {
                return cell.Column.Header?.ToString();
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private static T? GetVisualParent<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match)
            {
                return match;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private void CreateStockTemplate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = "plantilla-stock.xlsx",
                InitialDirectory = _paths.ExportDirectory
            };
            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            using var stream = File.Create(dialog.FileName);
            _excelService.WriteStockTemplate(stream, []);
            Log($"Plantilla creada: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void ImportStock_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return;
        }

        var dialog = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var fileName = dialog.FileName;
        ImportResult<StockImportRow>? result = null;
        await RunBusyAsync(async () =>
        {
            result = await Task.Run(() =>
            {
                using var stream = File.OpenRead(fileName);
                return _excelService.ReadStockRows(stream);
            });
        }, "Leyendo archivo de stock...");

        if (result is null)
        {
            return;
        }

        if (!result.IsValid)
        {
            Log("Importacion rechazada:");
            foreach (var error in result.Errors)
            {
                Log($"Fila {error.RowNumber}: {error.Message}");
            }
        }

        if (!StockImportPreviewDialog.Show(this, result) || !result.IsValid)
        {
            Status("Importacion cancelada.");
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _inventoryViewModel.ImportStockRowsAsync(result.Rows);
            RefreshAfterMutation();
            Log($"Importadas {result.Rows.Count} filas de stock.");
        }, "Importando stock...");
    }


    private async void ExportInventory_Click(object sender, RoutedEventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            var path = await _inventoryViewModel.ExportInventoryReportAsync(_paths.ExportDirectory);
            Log($"Reporte de inventario: {path}");
        }, "Exportando inventario...");
    }

    private async void ExportInventoryPdf_Click(object sender, RoutedEventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            var path = await _dashboardViewModel.ExportInventoryPdfReportAsync(_paths.ExportDirectory);
            Log($"Reporte PDF de inventario: {path}");
        }, "Exportando inventario PDF...");
    }

}
