using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using FPCalzados.App.Dialogs;
using FPCalzados.Domain;
using FPCalzados.Infrastructure;
using Microsoft.Win32;

namespace FPCalzados.App;

public partial class MainWindow
{
    private void LoadBusinessSettings()
    {
        var settings = _settingsViewModel.GetBusinessSettings();
        BusinessNameBox.Text = settings.Name;
        BusinessAddressBox.Text = settings.Address;
        BusinessPhoneBox.Text = settings.Phone;
        BusinessTaxIdBox.Text = settings.TaxId;
        BusinessTicketFooterBox.Text = settings.TicketFooter;
    }

    private async void SaveBusinessSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            var settings = new BusinessSettings
            {
                Id = 1,
                Name = BusinessNameBox.Text.Trim(),
                Address = BusinessAddressBox.Text.Trim(),
                Phone = BusinessPhoneBox.Text.Trim(),
                TaxId = BusinessTaxIdBox.Text.Trim(),
                TicketFooter = BusinessTicketFooterBox.Text.Trim()
            };

            await RunBusyAsync(async () =>
            {
                await _settingsViewModel.SaveBusinessSettingsAsync(settings);
                Status("Datos del negocio guardados.");
            }, "Guardando datos del negocio...");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void RefreshPaymentRules()
    {
        _paymentRules = _settingsViewModel.GetPaymentRules();
        SaleRuleBox.ItemsSource = _paymentRules;
        SaleRuleBox.SelectedIndex = _paymentRules.Count > 0 ? 0 : -1;
        RulesGrid.ItemsSource = _paymentRules;
        UpdateSalePricePreview();
    }

    private void RefreshAuditLogs_Click(object sender, RoutedEventArgs e) => RefreshAuditLogs();

    private void RefreshAuditLogs()
    {
        AuditLogsGrid.ItemsSource = _settingsViewModel.GetAuditLogs();
    }

    private async void ExportAuditLogs_Click(object sender, RoutedEventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            var path = await _settingsViewModel.ExportAuditLogReportAsync(_paths.ExportDirectory);
            Status($"Auditoría exportada: {path}");
        }, "Exportando auditoría...");
    }


    private void NewRule_Click(object sender, RoutedEventArgs e)
    {
        _editingRuleId = null;
        RulesGrid.SelectedItem = null;
        RuleNameBox.Clear();
        RuleMethodBox.SelectedValue = PaymentMethod.Cash;
        RulePercentBox.Text = "0";
        RuleInstallmentsBox.Text = "1";
        RuleNameBox.Focus();
        Status("Nueva regla: completa los datos y pulsa Guardar cambios.");
    }

    private void DeleteRule_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            if (RulesGrid.SelectedItem is not PaymentRule rule)
            {
                throw new InvalidOperationException("Selecciona en la grilla la regla que deseas eliminar.");
            }

            var confirmation = MessageBox.Show(this,
                $"¿Eliminar la regla '{rule.Name}'? Las ventas anteriores conservarán sus importes.",
                "Eliminar regla de pago", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _settingsViewModel.DeletePaymentRule(rule.Id);
            NewRule_Click(sender, e);
            RefreshPaymentRules();
            RefreshAuditLogs();
            Status($"Regla '{rule.Name}' eliminada.");
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void SaveRule_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            var editingRuleId = _editingRuleId;
            _settingsViewModel.SavePaymentRule(new PaymentRule
            {
                Id = editingRuleId ?? 0,
                Name = RuleNameBox.Text.Trim(),
                PaymentMethod = (PaymentMethod)RuleMethodBox.SelectedValue,
                PercentModifier = ParseDecimal(RulePercentBox.Text, "porcentaje"),
                Installments = Math.Max(1, ParseInt(RuleInstallmentsBox.Text, "cuotas")),
                IsActive = true
            });
            _editingRuleId = null;
            RefreshPaymentRules();
            RefreshAuditLogs();
            Status(editingRuleId is null ? "Regla creada." : "Cambios de la regla guardados.");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void RulesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RulesGrid.SelectedItem is not PaymentRule rule)
        {
            return;
        }

        _editingRuleId = rule.Id;
        RuleNameBox.Text = rule.Name;
        RuleMethodBox.SelectedValue = rule.PaymentMethod;
        RulePercentBox.Text = rule.PercentModifier.ToString(CultureInfo.InvariantCulture);
        RuleInstallmentsBox.Text = rule.Installments.ToString(CultureInfo.InvariantCulture);
    }

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            _settingsViewModel.ChangeRolePassword((UserRole)PasswordRoleBox.SelectedValue, NewPasswordBox.Password);
            NewPasswordBox.Clear();
            Status("Clave actualizada.");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void ManualBackup_Click(object sender, RoutedEventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            var path = await Task.Run(() => _backupService.CreateManualBackup());
            await _settingsViewModel.RecordManualBackupAsync(path);
            Status($"Backup creado: {path}");
        }, "Creando backup...");
    }

    private async void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar backup para restaurar",
                InitialDirectory = _paths.BackupDirectory,
                Filter = "Backups SQLite (*.sqlite)|*.sqlite|Todos los archivos (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            var confirmation = MessageBox.Show(
                this,
                "Se va a reemplazar la base actual por el backup seleccionado. Antes se creará una copia de seguridad de la base actual. ¿Continuar?",
                "Restaurar backup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                var result = await Task.Run(() => _backupService.RestoreBackup(dialog.FileName));
                await Task.Run(() => new DatabaseInitializer(_factory, new AuthService()).Initialize());
                await _settingsViewModel.RecordRestoreBackupAsync(result.RestoredFrom, result.SafetyBackupPath);
                _salesLoaded = false;
                _saleInventoryLoaded = false;
                RefreshAfterMutation();
                RefreshAuditLogs();
                Status($"Backup restaurado: {result.RestoredFrom}. Copia previa: {result.SafetyBackupPath}");
            }, "Restaurando backup...");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void ReviewDuplicateCustomers_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireAdmin();
            new CustomerDuplicatesDialog(this, _store).ShowDialog();
            RefreshAuditLogs();
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

}
