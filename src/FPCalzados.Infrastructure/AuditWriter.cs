using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

internal static class AuditWriter
{
    public static void Add(
        AppDbContext db,
        IClock clock,
        UserRole? role,
        string action,
        string entity,
        string? entityId,
        string message)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OccurredAt = clock.Now,
            Role = role,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Message = message
        });
    }
}
