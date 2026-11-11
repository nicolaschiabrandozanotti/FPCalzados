using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

public sealed partial class ApplicationStore
{
    public AuthSession Authenticate(UserRole role, string password)
    {
        var session = settings.Authenticate(role, password);
        currentRole = role;
        return session;
    }

    public void ChangeRolePassword(UserRole role, string password)
    {
        if (currentRole is not null && currentRole != UserRole.Admin && currentRole != role)
        {
            throw new UnauthorizedAccessException("Solo el Administrador puede cambiar la contraseña de otro rol.");
        }

        settings.ChangeRolePassword(role, password, currentRole);
    }

    public BusinessSettings GetBusinessSettings()
    {
        return settings.GetBusinessSettings();
    }

    public void SaveBusinessSettings(BusinessSettings businessSettings)
    {
        RequireAdmin();
        settings.SaveBusinessSettings(businessSettings, currentRole);
    }

    public IReadOnlyList<PaymentRule> GetPaymentRules()
    {
        return settings.GetPaymentRules();
    }

    public void SavePaymentRule(PaymentRule rule)
    {
        RequireAdmin();
        settings.SavePaymentRule(rule, currentRole);
    }

    public void DeletePaymentRule(int ruleId)
    {
        RequireAdmin();
        settings.DeletePaymentRule(ruleId, currentRole);
    }

    public IReadOnlyList<AuditLogRow> GetAuditLogs(int take = 300)
    {
        RequireAdmin();
        return audit.GetAuditLogs(take);
    }

    public string ExportAuditLogReport(string directory)
    {
        RequireAdmin();
        return audit.ExportAuditLogReport(directory);
    }

    public void RecordManualBackup(string path)
    {
        RecordAudit("Backup manual", "Backup", null, $"Backup creado: {path}");
    }

    public void RecordRestoreBackup(string restoredFrom, string safetyBackupPath)
    {
        RecordAudit("Restaurar backup", "Backup", null, $"Restaurado desde: {restoredFrom}. Copia previa: {safetyBackupPath}");
    }
}
