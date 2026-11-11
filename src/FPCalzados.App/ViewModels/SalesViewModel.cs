using System.Collections.ObjectModel;
using System.Globalization;
using FPCalzados.App;
using FPCalzados.Domain;
using FPCalzados.Infrastructure;

namespace FPCalzados.App.ViewModels;

public sealed record QuickSaleResult(bool Added, string Message, IReadOnlyList<InventoryRow> Matches);

public sealed class SalesViewModel(ApplicationStore store) : BaseViewModel
{
    private string _salesSearchText = "";
    private decimal _cartTotal;
    private string _cartBreakdown = "";
    private int _salesPage = 1;
    private int _salesTotalPages = 1;
    private int _salesTotalCount;
    private CancellationTokenSource? _salesLoadCancellation;

    public ObservableCollection<CartLine> Cart { get; } = [];
    public ObservableCollection<PaymentLine> Payments { get; } = [];
    public ObservableCollection<InventoryRow> SaleInventoryRows { get; } = [];
    public ObservableCollection<SaleRow> Sales { get; } = [];
    public ObservableCollection<CustomerDebtRow> CustomerDebts { get; } = [];
    public ObservableCollection<PaymentCollectionRow> PaymentCollections { get; } = [];

    public string SalesSearchText
    {
        get => _salesSearchText;
        set => SetProperty(ref _salesSearchText, value);
    }

    public int SalesPage
    {
        get => _salesPage;
        private set => SetProperty(ref _salesPage, value);
    }

    public int SalesTotalPages
    {
        get => _salesTotalPages;
        private set => SetProperty(ref _salesTotalPages, value);
    }

    public int SalesTotalCount
    {
        get => _salesTotalCount;
        private set => SetProperty(ref _salesTotalCount, value);
    }

    public string SalesPageLabel => $"Página {SalesPage} de {SalesTotalPages} · {SalesTotalCount} ventas";

    public decimal CartTotal
    {
        get => _cartTotal;
        private set => SetProperty(ref _cartTotal, value);
    }

    public string CartBreakdown
    {
        get => _cartBreakdown;
        private set => SetProperty(ref _cartBreakdown, value);
    }

    public void AddCartLine(InventoryRow selected, int quantity, decimal discount, PaymentRule rule)
    {
        var cashPrice = store.GetVariantCashPrice(selected.VariantId);
        var calculation = new PricingService().CalculatePayment(cashPrice, rule, discount);
        Cart.Add(new CartLine(
            selected.VariantId,
            selected.DisplayName,
            quantity,
            discount,
            rule.Id,
            rule.Name,
            rule.PaymentMethod,
            rule.Installments,
            calculation.InstallmentAmount,
            calculation.Total,
            calculation.Total * quantity));
        UpdateCartTotals();
    }

    public void RemoveCartLine(CartLine line)
    {
        Cart.Remove(line);
        UpdateCartTotals();
    }

    public void AddPayment(PaymentMethod method, decimal amount, int installments)
    {
        Payments.Add(new PaymentLine(method, amount, Math.Max(1, installments)));
    }

    public void RemovePayment(PaymentLine line)
    {
        Payments.Remove(line);
    }

    public void ClearSaleDraft()
    {
        Cart.Clear();
        Payments.Clear();
        UpdateCartTotals();
    }

    public IReadOnlyList<SaleLineDraft> BuildSaleLineDrafts()
    {
        return Cart
            .Select(item => new SaleLineDraft(item.VariantId, item.Quantity, item.ManualDiscount, item.PaymentRuleId))
            .ToList();
    }

    public IReadOnlyList<PaymentRequest> BuildPaymentRequests()
    {
        return Payments
            .Select(item => new PaymentRequest(item.Method, item.Amount, item.Installments))
            .ToList();
    }

    public Sale CreateSale(Customer? customer, DateOnly? dueDate)
    {
        if (Cart.Count == 0)
        {
            throw new InvalidOperationException("Agrega al menos un producto al carrito.");
        }

        return store.CreateSale(BuildSaleLineDrafts(), BuildPaymentRequests(), customer, dueDate);
    }

    public Task<Sale> CreateSaleAsync(Customer? customer, DateOnly? dueDate)
    {
        if (Cart.Count == 0)
        {
            throw new InvalidOperationException("Agrega al menos un producto al carrito.");
        }

        var lines = BuildSaleLineDrafts();
        var payments = BuildPaymentRequests();
        return Task.Run(() => store.CreateSale(lines, payments, customer, dueDate));
    }

    public SaleDetail GetSaleDetail(int saleId)
    {
        return store.GetSaleDetail(saleId);
    }

    public Task<SaleDetail> GetSaleDetailAsync(int saleId)
    {
        return Task.Run(() => store.GetSaleDetail(saleId));
    }

    public string ExportPaymentCollectionsReport(string directory, DateOnly from, DateOnly to)
    {
        return store.ExportPaymentCollectionsReport(directory, from, to);
    }

    public Task<string> ExportPaymentCollectionsReportAsync(string directory, DateOnly from, DateOnly to)
    {
        return Task.Run(() => store.ExportPaymentCollectionsReport(directory, from, to));
    }

    public string ExportCustomerDebtReport(string directory)
    {
        return store.ExportCustomerDebtReport(directory);
    }

    public Task<string> ExportCustomerDebtReportAsync(string directory)
    {
        return Task.Run(() => store.ExportCustomerDebtReport(directory));
    }

    public Task<CustomerHistory> GetCustomerHistoryAsync(string customer, string phone)
    {
        return Task.Run(() => store.GetCustomerHistory(customer, phone));
    }

    public string ExportSaleDetailReport(int saleId, string directory)
    {
        return store.ExportSaleDetailReport(saleId, directory);
    }

    public Task<string> ExportSaleDetailReportAsync(int saleId, string directory)
    {
        return Task.Run(() => store.ExportSaleDetailReport(saleId, directory));
    }

    public Task<string> BuildSaleReceiptTextAsync(int saleId)
    {
        return Task.Run(() => store.BuildSaleReceiptText(saleId));
    }

    public Task<string> ExportSaleReceiptTextAsync(int saleId, string directory)
    {
        return Task.Run(() => store.ExportSaleReceiptText(saleId, directory));
    }

    public void UpdateSaleDueDate(int saleId, DateOnly? dueDate)
    {
        store.UpdateSaleDueDate(saleId, dueDate);
    }

    public Task UpdateSaleDueDateAsync(int saleId, DateOnly? dueDate)
    {
        return Task.Run(() => store.UpdateSaleDueDate(saleId, dueDate));
    }

    public Payment RegisterPayment(int saleId, PaymentRequest request)
    {
        return store.RegisterPayment(saleId, request);
    }

    public Task<Payment> RegisterPaymentAsync(int saleId, PaymentRequest request)
    {
        return Task.Run(() => store.RegisterPayment(saleId, request));
    }

    public IReadOnlyList<ReturnableSaleItemRow> GetPendingReturnableSaleItems(int saleId)
    {
        var items = store.GetReturnableSaleItems(saleId)
            .Where(item => item.PendingReturnQuantity > 0)
            .ToList();
        if (items.Count == 0)
        {
            throw new InvalidOperationException("La venta no tiene items pendientes de devolver.");
        }

        return items;
    }

    public Task<IReadOnlyList<ReturnableSaleItemRow>> GetPendingReturnableSaleItemsAsync(int saleId)
    {
        return Task.Run(() => GetPendingReturnableSaleItems(saleId));
    }

    public ReturnRecord ReturnSaleItem(int saleId, int saleItemId, int quantity, string reason)
    {
        return store.ReturnSaleItem(saleId, saleItemId, quantity, reason);
    }

    public Task<ReturnRecord> ReturnSaleItemAsync(int saleId, int saleItemId, int quantity, string reason)
    {
        return Task.Run(() => store.ReturnSaleItem(saleId, saleItemId, quantity, reason));
    }

    public async Task LoadSaleInventoryAsync(string search)
    {
        var rows = await Task.Run(() => BuildSaleInventoryRows(store.GetInventory(search)));
        SetSaleInventoryRows(rows);
    }

    public async Task<QuickSaleResult> QuickAddSaleSearchAsync(string search, int quantity, decimal discount, PaymentRule rule)
    {
        var rows = await Task.Run(() => BuildSaleInventoryRows(store.GetInventory(search)));
        SetSaleInventoryRows(rows);
        if (rows.Count == 0)
        {
            return new QuickSaleResult(false, "No se encontraron productos con stock.", rows);
        }

        if (rows.Count > 1)
        {
            return new QuickSaleResult(false, $"Hay {rows.Count} coincidencias. Elegi talle/color y agregalo al carrito.", rows);
        }

        AddCartLine(rows[0], quantity, discount, rule);
        return new QuickSaleResult(true, $"Agregado rapido: {rows[0].DisplayName} x{quantity}.", rows);
    }

    public void LoadSaleInventory(string search)
    {
        SetSaleInventoryRows(BuildSaleInventoryRows(store.GetInventory(search)));
    }

    public async Task LoadSalesAsync(DateOnly from, DateOnly to, int page = 1)
    {
        _salesLoadCancellation?.Cancel();
        _salesLoadCancellation?.Dispose();
        _salesLoadCancellation = new CancellationTokenSource();
        var token = _salesLoadCancellation.Token;
        var search = SalesSearchText;
        var result = await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();
            return store.GetSalesPage(from, to, search, page);
        }, token);
        token.ThrowIfCancellationRequested();
        SetSalesPage(result);
    }

    public void LoadSales(DateOnly from, DateOnly to, int page = 1)
    {
        SetSalesPage(store.GetSalesPage(from, to, SalesSearchText, page));
    }

    public async Task LoadCustomerDebtsAsync()
    {
        var rows = await Task.Run(store.GetCustomerDebts);
        SetCustomerDebts(rows);
    }

    public void LoadCustomerDebts()
    {
        SetCustomerDebts(store.GetCustomerDebts());
    }

    public async Task LoadPaymentCollectionsAsync(DateOnly from, DateOnly to)
    {
        var rows = await Task.Run(() => store.GetPaymentCollections(from, to));
        SetPaymentCollections(rows);
    }

    public void LoadPaymentCollections(DateOnly from, DateOnly to)
    {
        SetPaymentCollections(store.GetPaymentCollections(from, to));
    }

    private static List<InventoryRow> BuildSaleInventoryRows(IEnumerable<InventoryRow> rows)
    {
        return rows
            .Where(item => item.StockQuantity > 0)
            .GroupBy(item => item.ProductId)
            .SelectMany(group =>
            {
                var productRows = group.ToList();
                return productRows.Any(item => item.HasOptionDetail)
                    ? productRows.Where(item => item.HasOptionDetail)
                    : productRows;
            })
            .OrderBy(item => item.Brand)
            .ThenBy(item => item.Model)
            .ThenBy(item => item.Size)
            .ThenBy(item => item.Color)
            .ToList();
    }

    private void SetSalesPage(PagedResult<SaleRow> result)
    {
        SalesPage = result.Page;
        SalesTotalPages = result.TotalPages;
        SalesTotalCount = result.TotalCount;
        OnPropertyChanged(nameof(SalesPageLabel));
        SetSales(result.Items);
    }

    private void UpdateCartTotals()
    {
        CartTotal = Cart.Sum(item => item.LineTotal);
        var breakdown = Cart
            .Where(item => item.PaymentMethod == PaymentMethod.Credit && item.Installments > 1)
            .GroupBy(item => new { item.RuleName, item.Installments })
            .Select(group =>
            {
                var groupTotal = group.Sum(item => item.LineTotal);
                var installment = groupTotal / group.Key.Installments;
                return $"{group.Key.RuleName}: {group.Key.Installments} cuotas de {installment:C0}";
            })
            .ToList();
        CartBreakdown = breakdown.Count == 0 ? "" : string.Join(" | ", breakdown);
    }

    private void SetSaleInventoryRows(IEnumerable<InventoryRow> rows)
    {
        SaleInventoryRows.Clear();
        foreach (var row in rows)
        {
            SaleInventoryRows.Add(row);
        }
    }

    private void SetSales(IEnumerable<SaleRow> rows)
    {
        Sales.Clear();
        foreach (var row in rows)
        {
            Sales.Add(row);
        }
    }

    private void SetCustomerDebts(IEnumerable<CustomerDebtRow> rows)
    {
        CustomerDebts.Clear();
        foreach (var row in rows)
        {
            CustomerDebts.Add(row);
        }
    }

    private void SetPaymentCollections(IEnumerable<PaymentCollectionRow> rows)
    {
        PaymentCollections.Clear();
        foreach (var row in rows)
        {
            PaymentCollections.Add(row);
        }
    }

    private static string FormatSaleStatus(SaleStatus status)
    {
        return status switch
        {
            SaleStatus.Open => "Abierta",
            SaleStatus.PartiallyPaid => "Pago parcial",
            SaleStatus.Paid => "Pagada",
            SaleStatus.Returned => "Devuelta",
            SaleStatus.Cancelled => "Cancelada",
            _ => status.ToString()
        };
    }
}
