using FPCalzados.Domain;
using Microsoft.EntityFrameworkCore;

namespace FPCalzados.Infrastructure;

internal sealed class SettingsApplicationService(AppDbContextFactory factory, IClock clock)
{
    public AuthSession Authenticate(UserRole role, string password)
    {
        using var db = factory.CreateDbContext();
        var credentials = db.RoleCredentials.ToList();
        try
        {
            return new AuthService().Login(credentials, role, password);
        }
        finally
        {
            db.SaveChanges();
        }
    }

    public void ChangeRolePassword(UserRole role, string password, UserRole? performedBy)
    {
        using var db = factory.CreateDbContext();
        var auth = new AuthService();
        var existing = db.RoleCredentials.SingleOrDefault(item => item.Role == role);
        if (existing is null)
        {
            db.RoleCredentials.Add(auth.CreateCredential(role, password));
        }
        else
        {
            auth.ChangePassword(existing, password, clock.Now);
        }

        AuditWriter.Add(db, clock, performedBy, "Cambiar clave", "RoleCredential", role.ToString(), $"Clave actualizada para {role}.");
        db.SaveChanges();
        if (!db.RoleCredentials.Any(item => item.RequiresPasswordChange))
        {
            DeleteInitialCredentialsFile();
        }
    }

    private void DeleteInitialCredentialsFile()
    {
        var dataDirectory = Path.GetDirectoryName(factory.DatabasePath)!;
        var rootDirectory = Directory.GetParent(dataDirectory)?.FullName ?? dataDirectory;
        var credentialsPath = Path.Combine(rootDirectory, "CREDENCIALES-INICIALES.txt");
        if (File.Exists(credentialsPath))
        {
            File.Delete(credentialsPath);
        }
    }

    public BusinessSettings GetBusinessSettings()
    {
        using var db = factory.CreateDbContext();
        return db.BusinessSettings.AsNoTracking().FirstOrDefault() ?? new BusinessSettings();
    }

    public void SaveBusinessSettings(BusinessSettings settings, UserRole? role)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            throw new InvalidOperationException("El nombre del negocio es obligatorio.");
        }

        using var db = factory.CreateDbContext();
        var existing = db.BusinessSettings.FirstOrDefault();
        if (existing is null)
        {
            settings.Id = 1;
            db.BusinessSettings.Add(settings);
        }
        else
        {
            existing.Name = settings.Name.Trim();
            existing.Address = settings.Address.Trim();
            existing.Phone = settings.Phone.Trim();
            existing.TaxId = settings.TaxId.Trim();
            existing.TicketFooter = settings.TicketFooter.Trim();
        }

        AuditWriter.Add(db, clock, role, "Guardar datos del negocio", "BusinessSettings", "1", $"Datos del negocio actualizados: {settings.Name}.");
        db.SaveChanges();
    }

    public IReadOnlyList<PaymentRule> GetPaymentRules()
    {
        using var db = factory.CreateDbContext();
        return db.PaymentRules.AsNoTracking()
            .Where(rule => rule.IsActive)
            .OrderBy(rule => rule.PaymentMethod)
            .ThenBy(rule => rule.Installments)
            .ToList();
    }

    public void DeletePaymentRule(int ruleId, UserRole? role)
    {
        using var db = factory.CreateDbContext();
        var rule = db.PaymentRules.Single(item => item.Id == ruleId);
        if (db.PaymentRules.Count(item => item.IsActive) <= 1)
        {
            throw new InvalidOperationException("Debe quedar al menos una regla de pago activa.");
        }

        rule.IsActive = false;
        AuditWriter.Add(db, clock, role, "Eliminar regla de pago", "PaymentRule", rule.Id.ToString(), $"Regla '{rule.Name}' desactivada.");
        db.SaveChanges();
    }

    public void SavePaymentRule(PaymentRule rule, UserRole? role)
    {
        using var db = factory.CreateDbContext();
        if (rule.Id == 0)
        {
            db.PaymentRules.Add(rule);
        }
        else
        {
            db.PaymentRules.Update(rule);
        }

        AuditWriter.Add(db, clock, role, "Guardar regla de pago", "PaymentRule", rule.Id == 0 ? null : rule.Id.ToString(), $"Regla '{rule.Name}' guardada.");
        db.SaveChanges();
    }
}
