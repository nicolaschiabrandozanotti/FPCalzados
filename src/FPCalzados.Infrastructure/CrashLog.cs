using System.Text.Json;
using System.Text.RegularExpressions;

namespace FPCalzados.Infrastructure;

public static partial class CrashLog
{
    private const int MaximumLogFiles = 30;

    public static string WriteException(PortablePaths paths, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        paths.EnsureDirectories();
        var path = Path.Combine(paths.LogDirectory, $"crash-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
        var entry = new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            level = "Critical",
            eventName = "UnhandledException",
            exceptionType = exception.GetType().FullName,
            message = Sanitize(exception.Message),
            stackTrace = Sanitize(exception.StackTrace),
            innerException = exception.InnerException is null
                ? null
                : new
                {
                    exceptionType = exception.InnerException.GetType().FullName,
                    message = Sanitize(exception.InnerException.Message)
                }
        };

        File.WriteAllText(path, JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true }));
        Prune(paths.LogDirectory);
        return path;
    }

    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return SensitiveValue().Replace(value, "$1=[REDACTED]");
    }

    private static void Prune(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "crash-*.json")
                     .Select(path => new FileInfo(path))
                     .OrderByDescending(file => file.CreationTimeUtc)
                     .Skip(MaximumLogFiles))
        {
            file.Delete();
        }

        // Compatibilidad: también limita logs de texto creados por versiones anteriores.
        foreach (var file in Directory.EnumerateFiles(directory, "crash-*.log")
                     .Select(path => new FileInfo(path))
                     .OrderByDescending(file => file.CreationTimeUtc)
                     .Skip(MaximumLogFiles))
        {
            file.Delete();
        }
    }

    [GeneratedRegex("(?i)(password|contraseña|token|secret)\\s*[:=]\\s*[^\\s,;]+")]
    private static partial Regex SensitiveValue();
}
