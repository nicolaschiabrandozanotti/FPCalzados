namespace FPCalzados.Domain;

public sealed class RoleCredential
{
    public int Id { get; set; }
    public UserRole Role { get; set; }
    public string PasswordHash { get; set; } = "";
    public bool RequiresPasswordChange { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}

public sealed record AuthSession(UserRole Role, DateTimeOffset LoggedInAt, bool RequiresPasswordChange = false);

public sealed class AuditLog
{
    public int Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.Now;
    public UserRole? Role { get; set; }
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string? EntityId { get; set; }
    public string Message { get; set; } = "";
}
