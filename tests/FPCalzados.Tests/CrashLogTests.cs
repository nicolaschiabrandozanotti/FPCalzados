using FPCalzados.Infrastructure;

namespace FPCalzados.Tests;

public sealed class CrashLogTests
{
    [Fact]
    public void WriteException_creates_log_file_with_exception_message()
    {
        var root = Path.Combine(Path.GetTempPath(), "fpcalzados-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var paths = PortablePaths.FromRoot(root);

            var logPath = CrashLog.WriteException(paths, new InvalidOperationException("fallo visible"));

            Assert.True(File.Exists(logPath));
            Assert.Contains("fallo visible", File.ReadAllText(logPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void OperationalLog_writes_structured_event_without_personal_payload()
    {
        var root = Path.Combine(Path.GetTempPath(), "fpcalzados-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var paths = PortablePaths.FromRoot(root);
            OperationalLog.Write(paths, "BackupCompleted", properties: new Dictionary<string, object?> { ["count"] = 1 });

            var logPath = Assert.Single(Directory.EnumerateFiles(paths.LogDirectory, "operational-*.jsonl"));
            var content = File.ReadAllText(logPath);
            Assert.Contains("BackupCompleted", content);
            Assert.Contains("\"count\":1", content);
            Assert.DoesNotContain("password", content, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void WriteException_redacts_secrets_and_rotates_old_logs()
    {
        var root = Path.Combine(Path.GetTempPath(), "fpcalzados-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var paths = PortablePaths.FromRoot(root);
            for (var index = 0; index < 35; index++)
            {
                CrashLog.WriteException(paths, new InvalidOperationException($"password=secret-{index}"));
            }

            var files = Directory.EnumerateFiles(paths.LogDirectory, "crash-*.json").ToList();
            Assert.Equal(30, files.Count);
            Assert.All(files, file =>
            {
                var content = File.ReadAllText(file);
                Assert.Contains("[REDACTED]", content);
                Assert.DoesNotContain("secret-", content);
            });
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
