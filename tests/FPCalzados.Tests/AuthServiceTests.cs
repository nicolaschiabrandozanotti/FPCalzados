using System.Security.Cryptography;
using FPCalzados.Domain;

namespace FPCalzados.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public void Login_accepts_role_password_and_rejects_wrong_password()
    {
        var service = new AuthService();
        var roles = new List<RoleCredential>
        {
            service.CreateCredential(UserRole.Admin, "admin12345"),
            service.CreateCredential(UserRole.Seller, "venta12345")
        };

        Assert.Equal(UserRole.Admin, service.Login(roles, UserRole.Admin, "admin12345").Role);
        Assert.Throws<UnauthorizedAccessException>(() => service.Login(roles, UserRole.Admin, "mal"));
    }

    [Fact]
    public void Login_locks_role_after_five_failed_attempts()
    {
        var service = new AuthService();
        var credential = service.CreateCredential(UserRole.Admin, "secure12345");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.Throws<UnauthorizedAccessException>(() => service.Login([credential], UserRole.Admin, "incorrecta"));
        }

        var exception = Assert.Throws<UnauthorizedAccessException>(() => service.Login([credential], UserRole.Admin, "secure12345"));
        Assert.Contains("bloqueado", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(credential.LockedUntil);
    }

    [Fact]
    public void Login_handles_malformed_hash_as_invalid_password()
    {
        var credential = new RoleCredential { Role = UserRole.Admin, PasswordHash = "invalid$hash" };

        Assert.Throws<UnauthorizedAccessException>(() => new AuthService().Login([credential], UserRole.Admin, "secure12345"));
    }

    [Fact]
    public void ChangePassword_clears_temporary_password_requirement()
    {
        var service = new AuthService();
        var credential = service.CreateCredential(UserRole.Admin, "temporary123", requiresPasswordChange: true);
        Assert.True(service.Login([credential], UserRole.Admin, "temporary123").RequiresPasswordChange);

        service.ChangePassword(credential, "permanent123", DateTimeOffset.Now);

        Assert.False(service.Login([credential], UserRole.Admin, "permanent123").RequiresPasswordChange);
        Assert.Throws<UnauthorizedAccessException>(() => service.Login([credential], UserRole.Admin, "temporary123"));
    }

    [Fact]
    public void Login_upgrades_legacy_pbkdf2_hash_after_successful_authentication()
    {
        const string password = "legacy12345";
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        var credential = new RoleCredential
        {
            Role = UserRole.Admin,
            PasswordHash = $"pbkdf2-sha256$100000${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}"
        };

        new AuthService().Login([credential], UserRole.Admin, password);

        Assert.StartsWith("pbkdf2-sha256$210000$", credential.PasswordHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("sololetraslargas")]
    [InlineData("123456789012")]
    public void CreateCredential_rejects_weak_passwords(string password)
    {
        Assert.Throws<ArgumentException>(() => new AuthService().CreateCredential(UserRole.Admin, password));
    }
}
