using System.Security.Cryptography;

namespace FPCalzados.Domain;

public sealed class AuthService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 210_000;
    private const int MaximumFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    public RoleCredential CreateCredential(UserRole role, string password, bool requiresPasswordChange = false)
    {
        ValidatePassword(password);
        return new RoleCredential
        {
            Role = role,
            PasswordHash = HashPassword(password),
            RequiresPasswordChange = requiresPasswordChange,
            UpdatedAt = DateTimeOffset.Now
        };
    }

    public AuthSession Login(IEnumerable<RoleCredential> credentials, UserRole role, string password)
    {
        var credential = credentials.SingleOrDefault(item => item.Role == role)
            ?? throw new UnauthorizedAccessException("Rol no configurado.");
        var now = DateTimeOffset.Now;

        if (credential.LockedUntil is not null && credential.LockedUntil > now)
        {
            throw new UnauthorizedAccessException($"Acceso bloqueado temporalmente. Intenta nuevamente después de las {credential.LockedUntil:HH:mm}.");
        }

        if (!VerifySafely(password, credential.PasswordHash, out var needsRehash))
        {
            credential.FailedLoginAttempts++;
            if (credential.FailedLoginAttempts >= MaximumFailedAttempts)
            {
                credential.LockedUntil = now.Add(LockoutDuration);
                credential.FailedLoginAttempts = 0;
            }

            throw new UnauthorizedAccessException("Contraseña incorrecta.");
        }

        credential.FailedLoginAttempts = 0;
        credential.LockedUntil = null;
        if (needsRehash)
        {
            credential.PasswordHash = HashPassword(password);
            credential.UpdatedAt = now;
        }

        return new AuthSession(role, now, credential.RequiresPasswordChange);
    }

    public void ChangePassword(RoleCredential credential, string password, DateTimeOffset changedAt)
    {
        ValidatePassword(password);
        credential.PasswordHash = HashPassword(password);
        credential.RequiresPasswordChange = false;
        credential.FailedLoginAttempts = 0;
        credential.LockedUntil = null;
        credential.UpdatedAt = changedAt;
    }

    public static string GenerateTemporaryPassword()
    {
        const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string characters = letters + digits + "!@#$%";
        var bytes = RandomNumberGenerator.GetBytes(16);
        var password = new char[16];
        password[0] = letters[bytes[0] % letters.Length];
        password[1] = digits[bytes[1] % digits.Length];
        for (var index = 2; index < password.Length; index++)
        {
            password[index] = characters[bytes[index] % characters.Length];
        }

        return new string(password);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    private static bool VerifySafely(string password, string encoded, out bool needsRehash)
    {
        needsRehash = false;
        try
        {
            var parts = encoded.Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" ||
                !int.TryParse(parts[1], out var iterations) || iterations <= 0 || iterations > 10_000_000)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            if (salt.Length < 8 || expected.Length < 16)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            needsRehash = iterations < Iterations;
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10 ||
            !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            throw new ArgumentException("La contraseña debe tener al menos 10 caracteres e incluir letras y números.", nameof(password));
        }
    }
}
