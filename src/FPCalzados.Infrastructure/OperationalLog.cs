using System.Text.Json;

namespace FPCalzados.Infrastructure;

public static class OperationalLog
{
    private const int MaximumLogFiles = 30;
    private static readonly object Sync = new();

    public static void Write(PortablePaths paths, string eventName, string level = "Information", IReadOnlyDictionary<string, object?>? properties = null)
    {
        paths.EnsureDirectories();
        var entry = new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            level,
            eventName,
            properties = properties ?? new Dictionary<string, object?>()
        };
        var line = JsonSerializer.Serialize(entry);
        var logPath = Path.Combine(paths.LogDirectory, $"operational-{DateTime.UtcNow:yyyyMMdd}.jsonl");
        lock (Sync)
        {
            File.AppendAllText(logPath, line + Environment.NewLine);
            foreach (var file in Directory.EnumerateFiles(paths.LogDirectory, "operational-*.jsonl")
                         .Select(path => new FileInfo(path))
                         .OrderByDescending(file => file.Name)
                         .Skip(MaximumLogFiles))
            {
                file.Delete();
            }
        }
    }
}
