using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FPCalzados.App.Dialogs;
using FPCalzados.App.ViewModels;
using FPCalzados.Domain;
using FPCalzados.Infrastructure;
using Microsoft.Win32;

namespace FPCalzados.App;

public partial class MainWindow : Window
{
    private readonly PortablePaths _paths;
    private readonly AppDbContextFactory _factory;
    private readonly ApplicationStore _store;
    private readonly SessionViewModel _sessionViewModel;
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly InventoryViewModel _inventoryViewModel;
    private readonly SalesViewModel _salesViewModel;
    private readonly SuppliersViewModel _suppliersViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly BackupService _backupService;
    private readonly ExcelImportExportService _excelService = new();
    private readonly ObservableCollection<ProductVariantEditRow> _productVariantEdits = [];
    private static readonly IReadOnlyList<RoleOption> RoleOptions =
    [
        new(UserRole.Admin, "Admin"),
        new(UserRole.Seller, "Vendedor")
    ];

    private static readonly IReadOnlyList<PaymentMethodOption> PaymentMethodOptions =
    [
        new(PaymentMethod.Cash, "Efectivo"),
        new(PaymentMethod.Debit, "Debito"),
        new(PaymentMethod.Credit, "Credito")
    ];

    private IReadOnlyList<PaymentRule> _paymentRules = [];
    private IReadOnlyList<SupplierRow> _suppliers = [];
    private IReadOnlyList<InventoryRow> _saleInventoryRows = [];
    private InventoryRow? _selectedSaleVariant;
    private bool _updatingSaleFilters;
    private UserRole? _currentRole;
    private int? _editingRuleId;
    private bool _salesLoaded;
    private bool _saleInventoryLoaded;

    public MainWindow()
    {
        InitializeComponent();
        Title = VersionInfo.Display;
        VersionText.Text = $"v{VersionInfo.Version}";
        WireDashboardViewEvents();
        WireSalesViewEvents();
        WireInventoryViewEvents();
        WireSupplierViewEvents();
        WireSettingsViewEvents();
        WireReportsViewEvents();

        _paths = PortablePaths.FromExecutable();
        _paths.EnsureDirectories();
        _factory = new AppDbContextFactory(_paths.DatabasePath);
        _store = new ApplicationStore(_factory, new PricingService(), new SystemClock());
        _sessionViewModel = new SessionViewModel(_store);
        _dashboardViewModel = new DashboardViewModel(_store);
        _inventoryViewModel = new InventoryViewModel(_store);
        _salesViewModel = new SalesViewModel(_store);
        _suppliersViewModel = new SuppliersViewModel(_store);
        _settingsViewModel = new SettingsViewModel(_store);
        _backupService = new BackupService(_paths);
        new DatabaseInitializer(_factory, new AuthService()).Initialize();
        _backupService.CreateDailyBackupIfNeeded();

        ConfigureStaticControls();
        BindCollections();
        SetDefaultDates();
        RefreshSuppliers();
        RefreshPaymentRules();
        LoadBusinessSettings();
        RefreshDashboard();
    }

    private void ConfigureStaticControls()
    {
        LoginRoleBox.ItemsSource = RoleOptions;
        LoginRoleBox.SelectedValue = UserRole.Admin;
        PasswordRoleBox.ItemsSource = RoleOptions;
        PasswordRoleBox.SelectedValue = UserRole.Admin;
        PaymentMethodBox.ItemsSource = PaymentMethodOptions;
        PaymentMethodBox.SelectedValue = PaymentMethod.Cash;
        RuleMethodBox.ItemsSource = PaymentMethodOptions;
        RuleMethodBox.SelectedValue = PaymentMethod.Cash;
    }

    private void BindCollections()
    {
        CartGrid.ItemsSource = _salesViewModel.Cart;
        PaymentsGrid.ItemsSource = _salesViewModel.Payments;
        VariantEditGrid.ItemsSource = _productVariantEdits;
        _productVariantEdits.CollectionChanged += ProductVariantEdits_CollectionChanged;
    }

    private void SetDefaultDates()
    {
        var today = DateTime.Today;
        DashboardFromPicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
        DashboardToPicker.SelectedDate = today;
        ReportFromPicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
        ReportToPicker.SelectedDate = today;
        SaleDueDatePicker.SelectedDate = today.AddDays(7);
        CollectionsFromPicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
        CollectionsToPicker.SelectedDate = today;
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var role = (UserRole)LoginRoleBox.SelectedValue;
            _sessionViewModel.Authenticate(role, LoginPasswordBox.Password);
            if (_sessionViewModel.RequiresPasswordChange)
            {
                var changeDialog = new RequiredPasswordChangeDialog(this);
                if (changeDialog.ShowDialog() != true)
                {
                    _sessionViewModel.Logout();
                    LoginErrorText.Text = "Debes cambiar la contraseña temporal para ingresar.";
                    return;
                }

                _sessionViewModel.ChangeCurrentPassword(changeDialog.NewPassword);
                MessageBox.Show(this, "Contraseña actualizada correctamente.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            _currentRole = _sessionViewModel.CurrentRole;
            RoleText.Text = $"Rol: {FormatUserRole(role)}";
            LoginPanel.Visibility = Visibility.Collapsed;
            MainShell.Visibility = Visibility.Visible;
            LoginErrorText.Text = "";
            Status($"Sesion iniciada como {role}.");
        }
        catch (Exception ex)
        {
            LoginErrorText.Text = ex.Message;
        }
    }

    private void LoginPasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        Login_Click(sender, new RoutedEventArgs());
        e.Handled = true;
    }

    private void ChangeCurrentPassword_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new RequiredPasswordChangeDialog(this, required: false);
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            _sessionViewModel.ChangeCurrentPassword(dialog.NewPassword);
            Status("Contraseña actualizada correctamente.");
            MessageBox.Show(this, "Contraseña actualizada correctamente.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        _sessionViewModel.Logout();
        _currentRole = null;
        LoginPasswordBox.Clear();
        MainShell.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            $"{VersionInfo.Display}\n\nBase local SQLite portable.\nCarpetas:\n- Datos: {_paths.DatabasePath}\n- Backups: {_paths.BackupDirectory}\n- Exportaciones: {_paths.ExportDirectory}",
            "Acerca de FPCalzados",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, MainTabs))
        {
            return;
        }

        if (MainTabs.SelectedIndex == 0)
        {
            await RefreshDashboardAsync();
        }
        else if (MainTabs.SelectedIndex == 1)
        {
            await EnsureSalesTabDataAsync();
            FocusTextInput(SaleSearchBox);
        }
        else if (MainTabs.SelectedIndex == 2)
        {
            await RefreshInventoryAsync();
            FocusTextInput(InventorySearchBox);
        }
        else if (MainTabs.SelectedIndex == 3)
        {
            RefreshSuppliers();
            FocusTextInput(SupplierNameBox);
        }
    }

    private void FocusTextInput(TextBox textBox)
    {
        Dispatcher.BeginInvoke(() =>
        {
            textBox.Focus();
            Keyboard.Focus(textBox);
            textBox.SelectAll();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private async void RefreshDashboard_Click(object sender, RoutedEventArgs e) => await RefreshDashboardAsync();

    private async void ExportCashRegister_Click(object sender, RoutedEventArgs e)
    {
        var from = GetDate(DashboardFromPicker);
        var to = GetDate(DashboardToPicker);
        await RunBusyAsync(async () =>
        {
            var path = await _dashboardViewModel.ExportCashRegisterReportAsync(_paths.ExportDirectory, from, to);
            Status($"Cierre de caja exportado: {path}");
        }, "Exportando cierre de caja...");
    }

    private async void OpenCashRegister_Click(object sender, RoutedEventArgs e)
    {
        var request = CashRegisterDialog.Show(this, "Abrir caja", "Efectivo inicial", 0m);
        if (request is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _dashboardViewModel.OpenCashRegisterAsync(request.Value.Amount, request.Value.Notes);
            await _dashboardViewModel.LoadAsync(GetDate(DashboardFromPicker), GetDate(DashboardToPicker));
            ApplyDashboard(_dashboardViewModel.Summary!, _dashboardViewModel.CashRegister!);
            Status("Caja abierta.");
        }, "Abriendo caja...");
    }

    private async void CloseCashRegister_Click(object sender, RoutedEventArgs e)
    {
        var suggestedAmount = _dashboardViewModel.CashRegister?.ExpectedCash ?? 0m;
        var request = CashRegisterDialog.Show(this, "Cerrar caja", "Efectivo real contado", suggestedAmount);
        if (request is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _dashboardViewModel.CloseCashRegisterAsync(request.Value.Amount, request.Value.Notes);
            await _dashboardViewModel.LoadAsync(GetDate(DashboardFromPicker), GetDate(DashboardToPicker));
            ApplyDashboard(_dashboardViewModel.Summary!, _dashboardViewModel.CashRegister!);
            Status("Caja cerrada.");
        }, "Cerrando caja...");
    }

    private async Task RefreshDashboardAsync()
    {
        var from = GetDate(DashboardFromPicker);
        var to = GetDate(DashboardToPicker);
        await RunBusyAsync(async () =>
        {
            await _dashboardViewModel.LoadAsync(from, to);
            ApplyDashboard(_dashboardViewModel.Summary!, _dashboardViewModel.CashRegister!);
        }, "Actualizando dashboard...");
    }

    private void RefreshDashboard()
    {
        _dashboardViewModel.Load(GetDate(DashboardFromPicker), GetDate(DashboardToPicker));
        ApplyDashboard(_dashboardViewModel.Summary!, _dashboardViewModel.CashRegister!);
    }

    private void ApplyDashboard(DashboardSummary summary, CashRegisterSummary cashRegister)
    {
        GrossSalesText.Text = Money(summary.GrossSales);
        NetSalesText.Text = Money(summary.NetSales);
        PaidSalesText.Text = Money(summary.PaidTotal);
        PendingSalesText.Text = Money(summary.PendingTotal);
        ReturnedSalesText.Text = Money(summary.ReturnedTotal);
        LowStockText.Text = summary.LowStockCount.ToString(CultureInfo.CurrentCulture);
        OverdueText.Text = summary.OverdueCount.ToString(CultureInfo.CurrentCulture);
        TopProductsGrid.ItemsSource = summary.TopProducts;
        AlertsGrid.ItemsSource = summary.Alerts;

        CashRegisterStatusText.Text = cashRegister.IsOpen ? "Abierta" : cashRegister.HasSession ? "Cerrada" : "Sin apertura";
        DashboardViewControl.OpenCashRegisterButton.IsEnabled = !cashRegister.IsOpen;
        DashboardViewControl.CloseCashRegisterButton.IsEnabled = cashRegister.IsOpen;
        CashRegisterSalesCountText.Text = cashRegister.SalesCount.ToString(CultureInfo.CurrentCulture);
        CashRegisterCashText.Text = Money(cashRegister.CashTotal);
        CashRegisterDebitText.Text = Money(cashRegister.DebitTotal);
        CashRegisterCreditText.Text = Money(cashRegister.CreditTotal);
        CashRegisterPaidText.Text = Money(cashRegister.PaidTotal);
        CashRegisterPendingText.Text = Money(cashRegister.PendingTotal);
        CashRegisterReturnedText.Text = Money(cashRegister.ReturnedTotal);
        CashRegisterCashToReturnText.Text = Money(cashRegister.CashToReturn);
        CashRegisterOpeningCashText.Text = Money(cashRegister.OpeningCash);
        CashRegisterExpectedCashText.Text = Money(cashRegister.ExpectedCash);
        CashRegisterClosingCashText.Text = cashRegister.ClosingCash is null ? "-" : Money(cashRegister.ClosingCash.Value);
        CashRegisterDifferenceText.Text = cashRegister.Difference is null ? "-" : Money(cashRegister.Difference.Value);
    }

    private async void ExportSales_Click(object sender, RoutedEventArgs e)
    {
        var from = GetDate(ReportFromPicker);
        var to = GetDate(ReportToPicker);
        await RunBusyAsync(async () =>
        {
            var path = await _dashboardViewModel.ExportSalesReportAsync(_paths.ExportDirectory, from, to);
            Log($"Reporte de ventas: {path}");
        }, "Exportando ventas...");
    }

    private async void ExportSalesPdf_Click(object sender, RoutedEventArgs e)
    {
        var from = GetDate(ReportFromPicker);
        var to = GetDate(ReportToPicker);
        await RunBusyAsync(async () =>
        {
            var path = await _dashboardViewModel.ExportSalesPdfReportAsync(_paths.ExportDirectory, from, to);
            Log($"Reporte PDF de ventas: {path}");
        }, "Exportando ventas PDF...");
    }

    private async void ExportProfitability_Click(object sender, RoutedEventArgs e)
    {
        var from = GetDate(ReportFromPicker);
        var to = GetDate(ReportToPicker);
        await RunBusyAsync(async () =>
        {
            var path = await _dashboardViewModel.ExportProfitabilityReportAsync(_paths.ExportDirectory, from, to);
            Log($"Reporte de rentabilidad estimada: {path}");
        }, "Exportando rentabilidad...");
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (MainShell.Visibility != Visibility.Visible)
        {
            return;
        }

        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        if (e.Key == Key.F2)
        {
            FocusNewSaleSearch();
            e.Handled = true;
        }
        else if (e.Key == Key.F3)
        {
            FocusSalesHistorySearch();
            e.Handled = true;
        }
        else if (e.Key == Key.F4)
        {
            FocusInventorySearch();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.F)
        {
            FocusSearchForCurrentContext();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.Enter)
        {
            if (MainTabs.SelectedIndex == 1 && SalesTabs.SelectedIndex == 0)
            {
                if (ReferenceEquals(Keyboard.FocusedElement, SaleSearchBox))
                {
                    QuickAddSale_Click(sender, e);
                }
                else
                {
                    AddCart_Click(sender, e);
                }

                e.Handled = true;
            }
            else if (MainTabs.SelectedIndex == 2)
            {
                SaveProduct_Click(sender, e);
                e.Handled = true;
            }
        }
        else if (ctrl && e.Key == Key.P && MainTabs.SelectedIndex == 1 && SalesTabs.SelectedIndex == 0)
        {
            AddPayment_Click(sender, e);
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.G)
        {
            SaveCurrentTab();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.R)
        {
            RefreshCurrentTab();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.L && MainTabs.SelectedIndex == 1 && SalesTabs.SelectedIndex == 0)
        {
            ClearSale();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.K)
        {
            ManualBackup_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            RemoveSelectedLineForCurrentContext();
            e.Handled = true;
        }
    }

    private void FocusNewSaleSearch()
    {
        MainTabs.SelectedIndex = 1;
        SalesTabs.SelectedIndex = 0;
        EnsureSalesTabData();
        FocusTextBox(SaleSearchBox);
    }

    private void FocusSalesHistorySearch()
    {
        MainTabs.SelectedIndex = 1;
        SalesTabs.SelectedIndex = 1;
        if (!_salesLoaded)
        {
            RefreshSales();
        }

        FocusTextBox(SalesSearchBox);
    }

    private void FocusInventorySearch()
    {
        MainTabs.SelectedIndex = 2;
        FocusTextBox(InventorySearchBox);
    }

    private void FocusSearchForCurrentContext()
    {
        if (MainTabs.SelectedIndex == 1 && SalesTabs.SelectedIndex == 1)
        {
            FocusTextBox(SalesSearchBox);
        }
        else if (MainTabs.SelectedIndex == 2)
        {
            FocusTextBox(InventorySearchBox);
        }
        else
        {
            FocusNewSaleSearch();
        }
    }

    private static void FocusTextBox(TextBox textBox)
    {
        textBox.Focus();
        textBox.SelectAll();
    }

    private void RemoveSelectedLineForCurrentContext()
    {
        if (MainTabs.SelectedIndex != 1 || SalesTabs.SelectedIndex != 0)
        {
            return;
        }

        if (CartGrid.IsKeyboardFocusWithin || CartGrid.SelectedItem is CartLine)
        {
            RemoveCart_Click(this, new RoutedEventArgs());
        }
        else if (PaymentsGrid.IsKeyboardFocusWithin || PaymentsGrid.SelectedItem is PaymentLine)
        {
            RemovePayment_Click(this, new RoutedEventArgs());
        }
    }

    private void SaveCurrentTab()
    {
        switch (MainTabs.SelectedIndex)
        {
            case 1:
                if (SalesTabs.SelectedIndex == 0)
                {
                    SaveSale_Click(this, new RoutedEventArgs());
                }
                break;
            case 2:
                SaveProduct_Click(this, new RoutedEventArgs());
                break;
            case 3:
                SaveSupplier_Click(this, new RoutedEventArgs());
                break;
            case 5:
                SaveRule_Click(this, new RoutedEventArgs());
                break;
        }
    }

    private void RefreshCurrentTab()
    {
        switch (MainTabs.SelectedIndex)
        {
            case 0:
                RefreshDashboard();
                break;
            case 1:
                if (SalesTabs.SelectedIndex == 0)
                {
                    RefreshSaleInventory();
                }
                else if (SalesTabs.SelectedIndex == 1)
                {
                    RefreshSales();
                }
                else if (SalesTabs.SelectedIndex == 2)
                {
                    RefreshCustomerDebts();
                }
                else
                {
                    RefreshPaymentCollections();
                }
                break;
            case 2:
                RefreshInventory();
                break;
            case 3:
                RefreshSuppliers();
                break;
            case 5:
                RefreshPaymentRules();
                LoadBusinessSettings();
                RefreshAuditLogs();
                break;
        }
    }

    private void RefreshAfterMutation()
    {
        RefreshInventory();
        RefreshSuppliers();
        _saleInventoryLoaded = false;
        RefreshPaymentRules();
        LoadBusinessSettings();
        RefreshDashboard();
        RefreshSales();
        RefreshCustomerDebts();
        RefreshPaymentCollections();
    }


}
