using FPCalzados.Domain;
using FPCalzados.Infrastructure;

namespace FPCalzados.App.ViewModels;

public sealed class SettingsViewModel(ApplicationStore store) : BaseViewModel
{
    public BusinessSettings GetBusinessSettings()
    {
        return store.GetBusinessSettings();
    }

    public Task SaveBusinessSettingsAsync(BusinessSettings settings)
    {
        return Task.Run(() => store.SaveBusinessSettings(settings));
    }

    public IReadOnlyList<PaymentRule> GetPaymentRules()
    {
        return store.GetPaymentRules();
    }

    public IReadOnlyList<AuditLogRow> GetAuditLogs(int take = 300)
    {
        return store.GetAuditLogs(take);
    }

    public void SavePaymentRule(PaymentRule rule)
    {
        store.SavePaymentRule(rule);
    }

    public Task SavePaymentRuleAsync(PaymentRule rule)
    {
        return Task.Run(() => store.SavePaymentRule(rule));
    }

    public void DeletePaymentRule(int ruleId)
    {
        store.DeletePaymentRule(ruleId);
    }

    public void ChangeRolePassword(UserRole role, string password)
    {
        store.ChangeRolePassword(role, password);
    }

    public Task ChangeRolePasswordAsync(UserRole role, string password)
    {
        return Task.Run(() => store.ChangeRolePassword(role, password));
    }

    public void RecordManualBackup(string path)
    {
        store.RecordManualBackup(path);
    }

    public Task RecordManualBackupAsync(string path)
    {
        return Task.Run(() => store.RecordManualBackup(path));
    }

    public Task RecordRestoreBackupAsync(string restoredFrom, string safetyBackupPath)
    {
        return Task.Run(() => store.RecordRestoreBackup(restoredFrom, safetyBackupPath));
    }

    public Task<string> ExportAuditLogReportAsync(string directory)
    {
        return Task.Run(() => store.ExportAuditLogReport(directory));
    }
}
