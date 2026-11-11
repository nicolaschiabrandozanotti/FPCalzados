using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using FPCalzados.App.ViewModels;
using FPCalzados.Domain;
using FPCalzados.Infrastructure;

namespace FPCalzados.App;

public partial class MainWindow
{
    private async void SalesTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, SalesTabs))
        {
            return;
        }

        await EnsureSalesTabDataAsync();
    }

    private void EnsureSalesTabData()
    {
        if (SalesTabs.SelectedIndex == 0 && !_saleInventoryLoaded)
        {
            RefreshSaleInventory();
        }
        else if (SalesTabs.SelectedIndex == 1 && !_salesLoaded)
        {
            RefreshSales();
        }
        else if (SalesTabs.SelectedIndex == 2)
        {
            RefreshCustomerDebts();
        }
        else if (SalesTabs.SelectedIndex == 3)
        {
            RefreshPaymentCollections();
        }
    }

    private async Task EnsureSalesTabDataAsync()
    {
        if (SalesTabs.SelectedIndex == 0 && !_saleInventoryLoaded)
        {
            await RefreshSaleInventoryAsync();
        }
        else if (SalesTabs.SelectedIndex == 1 && !_salesLoaded)
        {
            await RefreshSalesAsync();
        }
        else if (SalesTabs.SelectedIndex == 2)
        {
            await RefreshCustomerDebtsAsync();
        }
        else if (SalesTabs.SelectedIndex == 3)
        {
            await RefreshPaymentCollectionsAsync();
        }
    }


    private async void RefreshSaleInventory_Click(object sender, RoutedEventArgs e) => await RefreshSaleInventoryAsync();

    private async void QuickAddSale_Click(object sender, RoutedEventArgs e) => await QuickAddSaleAsync();

    private async Task QuickAddSaleAsync()
    {
        try
        {
            if (SaleRuleBox.SelectedItem is not PaymentRule rule)
            {
                throw new InvalidOperationException("Selecciona una regla de pago.");
            }

            var quantity = ParseSaleQuantity();
            var discount = ParseDecimal(SaleDiscountBox.Text, "descuento");
            QuickSaleResult? result = null;
            await RunBusyAsync(async () =>
            {
                result = await _salesViewModel.QuickAddSaleSearchAsync(SaleSearchBox.Text, quantity, discount, rule);
                ApplySaleInventoryRows(result.Matches);
                UpdateSaleTotal();
            }, "Agregando producto rapido...");

            if (result is not null)
            {
                Status(result.Message);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task RefreshSaleInventoryAsync()
    {
        var search = SaleSearchBox.Text;
        await RunBusyAsync(async () =>
        {
            await _salesViewModel.LoadSaleInventoryAsync(search);
            ApplySaleInventoryRows(_salesViewModel.SaleInventoryRows);
        }, "Actualizando productos de venta...");
    }

    private void RefreshSaleInventory()
    {
        _salesViewModel.LoadSaleInventory(SaleSearchBox.Text);
        ApplySaleInventoryRows(_salesViewModel.SaleInventoryRows);
    }

    private void ApplySaleInventoryRows(IReadOnlyList<InventoryRow> rows)
    {
        _saleInventoryRows = rows;
        RefreshSaleVariantFilters();
        ApplySaleVariantFilters();
        _saleInventoryLoaded = true;
    }

    private void RefreshSaleVariantFilters()
    {
        _updatingSaleFilters = true;
        SaleSizeFilterBox.ItemsSource = BuildFilterOptions(_saleInventoryRows.Select(item => item.Size));
        SaleColorFilterBox.ItemsSource = BuildFilterOptions(_saleInventoryRows.Select(item => item.Color));
        SaleSizeFilterBox.SelectedIndex = 0;
        SaleColorFilterBox.SelectedIndex = 0;
        _updatingSaleFilters = false;
    }

    private static List<string> BuildFilterOptions(IEnumerable<string> values)
    {
        return values
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .Prepend("Todos")
            .ToList();
    }

    private void SaleVariantFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingSaleFilters)
        {
            return;
        }

        ApplySaleVariantFilters();
    }

    private void ApplySaleVariantFilters()
    {
        var size = SaleSizeFilterBox.SelectedItem as string;
        var color = SaleColorFilterBox.SelectedItem as string;
        var rows = _saleInventoryRows
            .Where(item => IsFilterMatch(item.Size, size))
            .Where(item => IsFilterMatch(item.Color, color))
            .ToList();
        var groups = BuildSaleProductGroups(rows);
        SaleProductGrid.ItemsSource = groups;
        SaleProductGrid.SelectedIndex = groups.Count == 1 ? 0 : -1;
        _selectedSaleVariant = groups.Count == 1 && groups[0].Options.Count == 1 ? groups[0].Options[0] : null;
        UpdateSelectedSaleVariantText();
    }

    private static List<SaleProductGroup> BuildSaleProductGroups(IEnumerable<InventoryRow> rows)
    {
        return rows
            .GroupBy(item => item.ProductId)
            .Select(group => CreateSaleProductGroup(group.ToList()))
            .OrderBy(item => item.Brand)
            .ThenBy(item => item.Model)
            .ToList();
    }

    private static SaleProductGroup CreateSaleProductGroup(IReadOnlyList<InventoryRow> options)
    {
        var first = options[0];
        return new SaleProductGroup(
            first.ProductId,
            first.Brand,
            first.Model,
            SummarizeColors(options),
            SummarizeSizes(options),
            options.Sum(item => item.StockQuantity),
            SummarizeMoney(options.Select(item => item.SupplierCost)),
            SummarizeMoney(options.Select(item => item.CashPrice)),
            options);
    }

    private static string SummarizeColors(IEnumerable<InventoryRow> options)
    {
        var colors = options
            .Select(item => string.IsNullOrWhiteSpace(item.Color) ? "Sin color" : item.Color)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .ToList();
        return string.Join(", ", colors);
    }

    private static string SummarizeSizes(IEnumerable<InventoryRow> options)
    {
        var byColor = options
            .GroupBy(item => string.IsNullOrWhiteSpace(item.Color) ? "General" : item.Color)
            .OrderBy(group => group.Key);

        return string.Join(" | ", byColor.Select(group =>
        {
            var sizes = group
                .Select(item => string.IsNullOrWhiteSpace(item.Size) ? "Sin talle" : item.Size)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item);
            return $"{group.Key}: {string.Join(", ", sizes)}";
        }));
    }

    private static string SummarizeMoney(IEnumerable<decimal> values)
    {
        var ordered = values.Distinct().OrderBy(item => item).ToList();
        return ordered.Count == 1
            ? Money(ordered[0])
            : $"{Money(ordered[0])} - {Money(ordered[^1])}";
    }

    private static bool IsFilterMatch(string value, string? filter)
    {
        return string.IsNullOrWhiteSpace(filter) ||
               filter == "Todos" ||
               string.Equals(value, filter, StringComparison.OrdinalIgnoreCase);
    }

    private void SaleProductGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedSaleVariant = SaleProductGrid.SelectedItem is SaleProductGroup { Options.Count: 1 } group
            ? group.Options[0]
            : null;
        UpdateSelectedSaleVariantText();
        UpdateSalePricePreview();
    }

    private void SaleProductGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!ReferenceEquals(GetVisualParent<DataGrid>(e.OriginalSource as DependencyObject), SaleProductGrid))
        {
            return;
        }

        var row = GetVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row?.Item is not SaleProductGroup group)
        {
            return;
        }

        var collapse = row.DetailsVisibility == Visibility.Visible;
        CollapseSaleProductDetails();
        if (collapse)
        {
            SaleProductGrid.SelectedItem = null;
            _selectedSaleVariant = null;
            UpdateSelectedSaleVariantText();
            e.Handled = true;
            return;
        }

        row.DetailsVisibility = Visibility.Visible;
        row.IsSelected = true;
        SaleProductGrid.SelectedItem = group;
        _selectedSaleVariant = group.Options.Count == 1 ? group.Options[0] : null;
        UpdateSelectedSaleVariantText();
        e.Handled = true;
    }

    private void CollapseSaleProductDetails()
    {
        foreach (var item in SaleProductGrid.Items)
        {
            if (SaleProductGrid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row)
            {
                row.DetailsVisibility = Visibility.Collapsed;
            }
        }
    }

    private void SaleOptionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is InventoryRow row)
        {
            _selectedSaleVariant = row;
            UpdateSelectedSaleVariantText();
            UpdateSalePricePreview();
        }
    }

    private void SaleOptionGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is InventoryRow row)
        {
            _selectedSaleVariant = row;
            UpdateSelectedSaleVariantText();
        }

        AddCart_Click(sender, e);
        e.Handled = true;
    }

    private void SaleProductGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        AddCart_Click(sender, e);
        e.Handled = true;
    }

    private void UpdateSelectedSaleVariantText()
    {
        if (_selectedSaleVariant is not InventoryRow row)
        {
            SelectedSaleVariantText.Text = "Selecciona un producto y luego una opcion de talle/color para agregarla al carrito.";
            return;
        }

        var size = string.IsNullOrWhiteSpace(row.Size) ? "sin talle" : row.Size;
        var color = string.IsNullOrWhiteSpace(row.Color) ? "sin color" : row.Color;
        SelectedSaleVariantText.Text = $"Seleccionado: {row.Brand} {row.Model} | Talle: {size} | Color: {color} | Stock: {row.StockQuantity} | Precio: {Money(row.CashPrice)}";
    }

    private void SaleRuleBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSalePricePreview();

    private void SalePriceInput_Changed(object sender, TextChangedEventArgs e) => UpdateSalePricePreview();

    private void UpdateSalePricePreview()
    {
        if (SaleFinalPricePreviewText is null)
        {
            return;
        }

        var selected = ResolveSelectedSaleVariant();
        if (selected is null)
        {
            SaleFinalPricePreviewText.Text = "Selecciona una opcion para ver el precio final.";
            return;
        }

        if (SaleRuleBox.SelectedItem is not PaymentRule rule)
        {
            SaleFinalPricePreviewText.Text = "Selecciona una forma de precio.";
            return;
        }

        if (!TryParseInt(SaleQuantityBox.Text, out var quantity) ||
            !TryParseDecimal(SaleDiscountBox.Text, out var discount))
        {
            SaleFinalPricePreviewText.Text = "Cantidad o descuento invalido.";
            return;
        }

        quantity = Math.Max(0, Math.Min(1000, quantity));
        var calculation = new PricingService().CalculatePayment(selected.CashPrice, rule, discount);
        var total = calculation.Total * quantity;
        var installment = calculation.InstallmentAmount * quantity;
        var detail = rule.PaymentMethod == PaymentMethod.Credit && rule.Installments > 1
            ? $" | {rule.Installments} cuotas de {Money(installment)}"
            : "";
        SaleFinalPricePreviewText.Text = $"Precio final: {Money(calculation.Total)} unitario | Total x{quantity}: {Money(total)}{detail}";
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (ReferenceEquals(sender, SaleSearchBox))
        {
            await RefreshSaleInventoryAsync();
        }
        else if (ReferenceEquals(sender, InventorySearchBox))
        {
            await RefreshInventoryAsync();
        }
        else if (ReferenceEquals(sender, SalesSearchBox))
        {
            await RefreshSalesAsync();
        }

        e.Handled = true;
    }


    private async void RefreshSales_Click(object sender, RoutedEventArgs e) => await RefreshSalesAsync();

    private async void RefreshCustomerDebts_Click(object sender, RoutedEventArgs e) => await RefreshCustomerDebtsAsync();

    private async void RefreshPaymentCollections_Click(object sender, RoutedEventArgs e) => await RefreshPaymentCollectionsAsync();

    private async void ExportPaymentCollections_Click(object sender, RoutedEventArgs e)
    {
        var from = GetDate(CollectionsFromPicker);
        var to = GetDate(CollectionsToPicker);
        await RunBusyAsync(async () =>
        {
            var path = await _salesViewModel.ExportPaymentCollectionsReportAsync(_paths.ExportDirectory, from, to);
            Status($"Cobranzas exportadas: {path}");
        }, "Exportando cobranzas...");
    }

    private async Task RefreshPaymentCollectionsAsync()
    {
        var from = GetDate(CollectionsFromPicker);
        var to = GetDate(CollectionsToPicker);
        await RunBusyAsync(async () =>
        {
            await _salesViewModel.LoadPaymentCollectionsAsync(from, to);
            PaymentCollectionsGrid.ItemsSource = _salesViewModel.PaymentCollections;
        }, "Actualizando cobranzas...");
    }

    private void RefreshPaymentCollections()
    {
        _salesViewModel.LoadPaymentCollections(GetDate(CollectionsFromPicker), GetDate(CollectionsToPicker));
        PaymentCollectionsGrid.ItemsSource = _salesViewModel.PaymentCollections;
    }

    private async void ExportCustomerDebts_Click(object sender, RoutedEventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            var path = await _salesViewModel.ExportCustomerDebtReportAsync(_paths.ExportDirectory);
            Status($"Deudas exportadas: {path}");
        }, "Exportando deudas...");
    }

    private async void ViewCustomerHistory_Click(object sender, RoutedEventArgs e) => await ShowSelectedCustomerHistoryAsync();

    private async void CustomerDebtsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => await ShowSelectedCustomerHistoryAsync();

    private async Task ShowSelectedCustomerHistoryAsync()
    {
        try
        {
            if (CustomerDebtsGrid.SelectedItem is not CustomerDebtRow row)
            {
                throw new InvalidOperationException("Selecciona un cliente.");
            }

            CustomerHistory? history = null;
            await RunBusyAsync(async () =>
            {
                history = await _salesViewModel.GetCustomerHistoryAsync(row.Customer, row.Phone);
            }, "Cargando historial del cliente...");

            if (history is not null)
            {
                CustomerHistoryDialog.Show(this, history);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task RefreshCustomerDebtsAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _salesViewModel.LoadCustomerDebtsAsync();
            CustomerDebtsGrid.ItemsSource = _salesViewModel.CustomerDebts;
        }, "Actualizando deudas...");
    }

    private void RefreshCustomerDebts()
    {
        _salesViewModel.LoadCustomerDebts();
        CustomerDebtsGrid.ItemsSource = _salesViewModel.CustomerDebts;
    }

    private async Task RefreshSalesAsync(int page = 1)
    {
        var from = GetDate(DashboardFromPicker);
        var to = GetDate(DashboardToPicker);
        _salesViewModel.SalesSearchText = SalesSearchBox.Text;
        await RunBusyAsync(async () =>
        {
            await _salesViewModel.LoadSalesAsync(from, to, page);
            ApplySalesRows(_salesViewModel.Sales);
        }, "Actualizando ventas...");
    }

    private void RefreshSales()
    {
        _salesViewModel.SalesSearchText = SalesSearchBox.Text;
        _salesViewModel.LoadSales(GetDate(DashboardFromPicker), GetDate(DashboardToPicker));
        ApplySalesRows(_salesViewModel.Sales);
    }

    private void ApplySalesRows(IReadOnlyList<SaleRow> sales)
    {
        SalesGrid.ItemsSource = sales;
        SalesPageText.Text = _salesViewModel.SalesPageLabel;
        _salesLoaded = true;
    }


    private async void PreviousSalesPage_Click(object sender, RoutedEventArgs e)
    {
        if (_salesViewModel.SalesPage > 1)
        {
            await RefreshSalesAsync(_salesViewModel.SalesPage - 1);
        }
    }

    private async void NextSalesPage_Click(object sender, RoutedEventArgs e)
    {
        if (_salesViewModel.SalesPage < _salesViewModel.SalesTotalPages)
        {
            await RefreshSalesAsync(_salesViewModel.SalesPage + 1);
        }
    }

    private void AddCart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selected = ResolveSelectedSaleVariant();
            if (selected is null)
            {
                throw new InvalidOperationException("Selecciona una opcion de talle/color dentro del producto.");
            }

            if (SaleRuleBox.SelectedItem is not PaymentRule rule)
            {
                throw new InvalidOperationException("Selecciona una regla de pago.");
            }

            var quantity = ParseSaleQuantity();
            var discount = ParseDecimal(SaleDiscountBox.Text, "descuento");
            _salesViewModel.AddCartLine(selected, quantity, discount, rule);
            UpdateSaleTotal();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private InventoryRow? ResolveSelectedSaleVariant()
    {
        if (_selectedSaleVariant is not null)
        {
            return _selectedSaleVariant;
        }

        if (SaleProductGrid.SelectedItem is SaleProductGroup { Options.Count: 1 } group)
        {
            _selectedSaleVariant = group.Options[0];
            UpdateSelectedSaleVariantText();
            return _selectedSaleVariant;
        }

        return null;
    }

    private void RemoveCart_Click(object sender, RoutedEventArgs e)
    {
        if (CartGrid.SelectedItem is CartLine line)
        {
            _salesViewModel.RemoveCartLine(line);
            UpdateSaleTotal();
        }
    }

    private void AddPayment_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _salesViewModel.AddPayment(
                (PaymentMethod)PaymentMethodBox.SelectedValue,
                ParseDecimal(PaymentAmountBox.Text, "monto"),
                Math.Max(1, ParseInt(PaymentInstallmentsBox.Text, "cuotas")));
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void RemovePayment_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentsGrid.SelectedItem is PaymentLine line)
        {
            _salesViewModel.RemovePayment(line);
        }
    }

    private async void SaveSale_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var customer = BuildCustomer();
            DateOnly? dueDate = SaleDueDatePicker.SelectedDate is null ? null : DateOnly.FromDateTime(SaleDueDatePicker.SelectedDate.Value);
            Sale? createdSale = null;
            await RunBusyAsync(async () =>
            {
                createdSale = await _salesViewModel.CreateSaleAsync(customer, dueDate);
                ClearSale();
                RefreshAfterMutation();
                Status($"Venta guardada. Neto: {Money(createdSale.NetTotal)} - Saldo: {Money(createdSale.BalanceDue)}");
            }, "Guardando venta...");

            if (createdSale is not null && MessageBox.Show(this, "Venta guardada. ¿Imprimir ticket?", "FPCalzados", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await PrintSaleReceiptAsync(createdSale.Id);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void ClearSale_Click(object sender, RoutedEventArgs e) => ClearSale();

    private void ClearSale()
    {
        _salesViewModel.ClearSaleDraft();
        CustomerFirstNameBox.Text = "";
        CustomerLastNameBox.Text = "";
        CustomerPhoneBox.Text = "";
        PaymentAmountBox.Text = "0";
        UpdateSaleTotal();
    }


    private async void ViewSaleDetail_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (SalesGrid.SelectedItem is not SaleRow sale)
            {
                throw new InvalidOperationException("Selecciona una venta.");
            }

            SaleDetail? detail = null;
            await RunBusyAsync(async () =>
            {
                detail = await _salesViewModel.GetSaleDetailAsync(sale.SaleId);
            }, "Cargando detalle de venta...");
            if (detail is not null)
            {
                SaleDetailDialog.Show(this, detail);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void ExportSaleDetail_Click(object sender, RoutedEventArgs e)
    {
        if (SalesGrid.SelectedItem is not SaleRow sale)
        {
            ShowError(new InvalidOperationException("Selecciona una venta."));
            return;
        }

        await RunBusyAsync(async () =>
        {
            var path = await _salesViewModel.ExportSaleDetailReportAsync(sale.SaleId, _paths.ExportDirectory);
            Status($"Comprobante exportado: {path}");
        }, "Exportando comprobante...");
    }

    private async void ExportSaleReceipt_Click(object sender, RoutedEventArgs e)
    {
        if (SalesGrid.SelectedItem is not SaleRow sale)
        {
            ShowError(new InvalidOperationException("Selecciona una venta."));
            return;
        }

        await RunBusyAsync(async () =>
        {
            var path = await _salesViewModel.ExportSaleReceiptTextAsync(sale.SaleId, _paths.ExportDirectory);
            Status($"Ticket exportado: {path}");
        }, "Exportando ticket...");
    }

    private async void PrintSaleReceipt_Click(object sender, RoutedEventArgs e)
    {
        if (SalesGrid.SelectedItem is not SaleRow sale)
        {
            ShowError(new InvalidOperationException("Selecciona una venta."));
            return;
        }

        await PrintSaleReceiptAsync(sale.SaleId);
    }

    private async Task PrintSaleReceiptAsync(int saleId)
    {
        string? receipt = null;
        await RunBusyAsync(async () =>
        {
            receipt = await _salesViewModel.BuildSaleReceiptTextAsync(saleId);
        }, "Preparando ticket...");

        if (string.IsNullOrWhiteSpace(receipt))
        {
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var document = new FlowDocument(new Paragraph(new Run(receipt)))
        {
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 12,
            PagePadding = new Thickness(24),
            ColumnWidth = dialog.PrintableAreaWidth,
            PageWidth = dialog.PrintableAreaWidth,
            PageHeight = dialog.PrintableAreaHeight
        };
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Ticket venta #{saleId:000000}");
        Status($"Ticket enviado a imprimir: venta #{saleId:000000}");
    }

    private async void ChangeDueDate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (SalesGrid.SelectedItem is not SaleRow sale)
            {
                throw new InvalidOperationException("Selecciona una venta.");
            }

            var dueDate = DueDateDialog.Show(this, sale.DueDate, out var accepted);
            if (!accepted)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                await _salesViewModel.UpdateSaleDueDateAsync(sale.SaleId, dueDate);
                RefreshAfterMutation();
                Status($"Vencimiento actualizado para venta #{sale.SaleId}.");
            }, "Actualizando vencimiento...");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void RegisterPayment_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (SalesGrid.SelectedItem is not SaleRow sale)
            {
                throw new InvalidOperationException("Selecciona una venta.");
            }

            if (sale.BalanceDue <= 0m)
            {
                throw new InvalidOperationException("La venta seleccionada no tiene saldo pendiente.");
            }

            var request = PaymentDialog.Show(this, PaymentMethodOptions, sale.BalanceDue);
            if (request is null)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                var payment = await _salesViewModel.RegisterPaymentAsync(
                    sale.SaleId,
                    new PaymentRequest(request.Value.Method, request.Value.Amount, request.Value.Installments));
                RefreshAfterMutation();
                Status($"Pago registrado: {Money(payment.Amount)}");
            }, "Registrando pago...");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void ReturnSelectedSale_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            if (SalesGrid.SelectedItem is not SaleRow sale)
            {
                throw new InvalidOperationException("Selecciona una venta.");
            }

            IReadOnlyList<ReturnableSaleItemRow>? items = null;
            await RunBusyAsync(async () =>
            {
                items = await _salesViewModel.GetPendingReturnableSaleItemsAsync(sale.SaleId);
            }, "Cargando productos a devolver...");
            if (items is null)
            {
                return;
            }

            var request = ReturnDialog.Show(this, items);
            if (request is null)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                var record = await _salesViewModel.ReturnSaleItemAsync(sale.SaleId, request.Value.SaleItemId, request.Value.Quantity, request.Value.Reason);
                RefreshAfterMutation();
                Status($"Devolucion registrada. Dinero a devolver: {Money(record.CashToReturn)}");
            }, "Registrando devolución...");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }


    private Customer? BuildCustomer()
    {
        var hasCustomer = !string.IsNullOrWhiteSpace(CustomerFirstNameBox.Text) ||
                          !string.IsNullOrWhiteSpace(CustomerLastNameBox.Text) ||
                          !string.IsNullOrWhiteSpace(CustomerPhoneBox.Text);
        if (!hasCustomer)
        {
            return null;
        }

        return new Customer
        {
            FirstName = CustomerFirstNameBox.Text.Trim(),
            LastName = CustomerLastNameBox.Text.Trim(),
            Phone = CustomerPhoneBox.Text.Trim()
        };
    }


    private void UpdateSaleTotal()
    {
        SaleTotalText.Text = Money(_salesViewModel.CartTotal);
        SaleCartBreakdownText.Text = _salesViewModel.CartBreakdown;
    }
}
