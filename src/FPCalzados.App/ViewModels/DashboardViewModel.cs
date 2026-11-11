using FPCalzados.Infrastructure;

namespace FPCalzados.App.ViewModels;

public sealed class DashboardViewModel(ApplicationStore store) : BaseViewModel
{
    private DashboardSummary? _summary;
    private CashRegisterSummary? _cashRegister;

    public AsyncRelayCommand RefreshCommand { get; } = new(_ => Task.CompletedTask);

    public DashboardSummary? Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public CashRegisterSummary? CashRegister
    {
        get => _cashRegister;
        private set => SetProperty(ref _cashRegister, value);
    }

    public async Task LoadAsync(DateOnly from, DateOnly to)
    {
        var data = await Task.Run(() => (store.GetDashboard(from, to), store.GetCashRegister(from, to)));
        Summary = data.Item1;
        CashRegister = data.Item2;
    }

    public void Load(DateOnly from, DateOnly to)
    {
        Summary = store.GetDashboard(from, to);
        CashRegister = store.GetCashRegister(from, to);
    }

    public Task<string> ExportCashRegisterReportAsync(string directory, DateOnly from, DateOnly to)
    {
        return Task.Run(() => store.ExportCashRegisterReport(directory, from, to));
    }

    public Task OpenCashRegisterAsync(decimal openingCash, string? notes)
    {
        return Task.Run(() => store.OpenCashRegister(openingCash, notes));
    }

    public Task CloseCashRegisterAsync(decimal closingCash, string? notes)
    {
        return Task.Run(() => store.CloseCashRegister(closingCash, notes));
    }

    public Task<string> ExportSalesReportAsync(string directory, DateOnly from, DateOnly to)
    {
        return Task.Run(() => store.ExportSalesReport(directory, from, to));
    }

    public Task<string> ExportSalesPdfReportAsync(string directory, DateOnly from, DateOnly to)
    {
        return Task.Run(() => store.ExportSalesPdfReport(directory, from, to));
    }

    public Task<string> ExportInventoryPdfReportAsync(string directory)
    {
        return Task.Run(() => store.ExportInventoryPdfReport(directory));
    }

    public Task<string> ExportProfitabilityReportAsync(string directory, DateOnly from, DateOnly to)
    {
        return Task.Run(() => store.ExportProfitabilityReport(directory, from, to));
    }
}
