using System.Collections.ObjectModel;
using FPCalzados.Domain;
using FPCalzados.Infrastructure;

namespace FPCalzados.App.ViewModels;

public sealed class InventoryViewModel : BaseViewModel
{
    private readonly ApplicationStore _store;
    private string _searchText = "";
    private string _stockMovementFilterText = "Últimos 200 movimientos";

    public InventoryViewModel(ApplicationStore store)
    {
        _store = store;
        RefreshCommand = new AsyncRelayCommand(_ => LoadInventoryAsync());
        RefreshStockMovementsCommand = new AsyncRelayCommand(_ => LoadStockMovementsAsync());
    }

    public ObservableCollection<InventoryRow> Rows { get; } = [];
    public ObservableCollection<StockMovementRow> StockMovements { get; } = [];
    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand RefreshStockMovementsCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string StockMovementFilterText
    {
        get => _stockMovementFilterText;
        private set => SetProperty(ref _stockMovementFilterText, value);
    }

    public async Task LoadInventoryAsync()
    {
        var search = SearchText;
        var data = await Task.Run(() => (_store.GetInventory(search), _store.GetStockMovements(take: 200)));
        SetRows(data.Item1);
        SetStockMovements(data.Item2, "Últimos 200 movimientos");
    }

    public void LoadInventory()
    {
        SetRows(_store.GetInventory(SearchText));
        LoadStockMovements();
    }

    public async Task LoadStockMovementsAsync()
    {
        var movements = await Task.Run(() => _store.GetStockMovements(take: 200));
        SetStockMovements(movements, "Últimos 200 movimientos");
    }

    public async Task LoadLowStockAsync()
    {
        var rows = await Task.Run(() => _store.GetInventory().Where(item => item.IsLowStock).ToList());
        SetRows(rows);
    }

    public void LoadStockMovements()
    {
        SetStockMovements(_store.GetStockMovements(take: 200), "Últimos 200 movimientos");
    }

    public async Task LoadStockMovementsForProductAsync(InventoryRow? row)
    {
        if (row is null)
        {
            await LoadStockMovementsAsync();
            return;
        }

        var productId = row.ProductId;
        var filterText = $"Producto: {row.Brand} {row.Model}";
        var movements = await Task.Run(() => _store.GetStockMovements(productId: productId, take: 200));
        SetStockMovements(movements, filterText);
    }

    public void LoadStockMovementsForProduct(InventoryRow? row)
    {
        if (row is null)
        {
            LoadStockMovements();
            return;
        }

        SetStockMovements(
            _store.GetStockMovements(productId: row.ProductId, take: 200),
            $"Producto: {row.Brand} {row.Model}");
    }

    public IReadOnlyList<InventoryRow> GetProductOptions(int productId)
    {
        return _store.GetInventory()
            .Where(item => item.ProductId == productId)
            .OrderBy(item => item.Color)
            .ThenBy(item => item.Size)
            .ToList();
    }

    public void SaveProductStock(ProductStockEditRequest request)
    {
        _store.SaveProductStock(request);
    }

    public Task SaveProductStockAsync(ProductStockEditRequest request)
    {
        return Task.Run(() => _store.SaveProductStock(request));
    }

    public void DeactivateVariant(int variantId)
    {
        _store.DeactivateVariant(variantId);
    }

    public Task DeactivateVariantAsync(int variantId)
    {
        return Task.Run(() => _store.DeactivateVariant(variantId));
    }

    public Task AdjustVariantStockAsync(int variantId, int quantityChange, StockMovementType type, string reason)
    {
        return Task.Run(() => _store.AdjustVariantStock(variantId, quantityChange, type, reason));
    }

    public void ImportStockRows(IReadOnlyList<StockImportRow> rows)
    {
        _store.ImportStockRows(rows);
    }

    public Task ImportStockRowsAsync(IReadOnlyList<StockImportRow> rows)
    {
        return Task.Run(() => _store.ImportStockRows(rows));
    }

    public Task<string> ExportInventoryReportAsync(string directory)
    {
        return Task.Run(() => _store.ExportInventoryReport(directory));
    }

    public Task<string> ExportStockMovementsReportAsync(string directory, int? productId = null)
    {
        return Task.Run(() => _store.ExportStockMovementsReport(directory, productId: productId));
    }

    private void SetRows(IEnumerable<InventoryRow> rows)
    {
        Rows.Clear();
        foreach (var row in rows)
        {
            Rows.Add(row);
        }
    }

    private void SetStockMovements(IEnumerable<StockMovementRow> movements, string filterText)
    {
        StockMovements.Clear();
        foreach (var movement in movements)
        {
            StockMovements.Add(movement);
        }

        StockMovementFilterText = filterText;
    }
}
