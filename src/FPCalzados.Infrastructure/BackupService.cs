using Microsoft.Data.Sqlite;

namespace FPCalzados.Infrastructure;

public sealed record RestoreBackupResult(string RestoredFrom, string SafetyBackupPath);

public sealed class BackupService
{
    private readonly PortablePaths paths;
    private readonly Action? afterDatabaseReplace;

    public BackupService(PortablePaths paths) : this(paths, null)
    {
    }

    internal BackupService(PortablePaths paths, Action? afterDatabaseReplace)
    {
        this.paths = paths;
        this.afterDatabaseReplace = afterDatabaseReplace;
    }

    private static readonly string[] RequiredTables =
    [
        "Products", "ProductVariants", "Sales", "SaleItems", "Payments", "RoleCredentials"
    ];

    public string CreateManualBackup()
    {
        paths.EnsureDirectories();
        EnsureDatabaseExists();
        return CreateBackupWithPrefix("FPCalzados-backup");
    }

    public RestoreBackupResult RestoreBackup(string backupPath)
    {
        paths.EnsureDirectories();
        if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
        {
            throw new FileNotFoundException("No existe el archivo de backup seleccionado.", backupPath);
        }

        if (!string.Equals(Path.GetExtension(backupPath), ".sqlite", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("El backup debe ser un archivo .sqlite.");
        }

        EnsureDatabaseExists();
        ValidateDatabase(backupPath, requireApplicationSchema: true);

        var restoreCandidate = Path.Combine(
            Path.GetDirectoryName(paths.DatabasePath)!,
            $".{Path.GetFileName(paths.DatabasePath)}.restore-{Guid.NewGuid():N}.tmp");

        string? safetyBackup = null;
        var databaseReplaced = false;
        try
        {
            File.Copy(backupPath, restoreCandidate, overwrite: false);
            ValidateDatabase(restoreCandidate, requireApplicationSchema: true);

            safetyBackup = CreateBackupWithPrefix("FPCalzados-pre-restore");
            File.Replace(restoreCandidate, paths.DatabasePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            databaseReplaced = true;
            afterDatabaseReplace?.Invoke();
            ValidateDatabase(paths.DatabasePath, requireApplicationSchema: true);
            OperationalLog.Write(paths, "DatabaseRestoreCompleted", properties: new Dictionary<string, object?>
            {
                ["backupFile"] = Path.GetFileName(backupPath),
                ["safetyBackupFile"] = Path.GetFileName(safetyBackup)
            });
            return new RestoreBackupResult(backupPath, safetyBackup);
        }
        catch
        {
            TryDelete(restoreCandidate);
            if (databaseReplaced && safetyBackup is not null)
            {
                RestoreSafetyCopy(safetyBackup);
            }

            throw;
        }
    }

    public string? CreateDailyBackupIfNeeded()
    {
        paths.EnsureDirectories();
        if (!File.Exists(paths.DatabasePath))
        {
            return null;
        }

        var todayPrefix = $"FPCalzados-auto-{DateTime.Today:yyyyMMdd}";
        if (Directory.EnumerateFiles(paths.BackupDirectory, $"{todayPrefix}*.sqlite").Any())
        {
            return null;
        }

        var target = Path.Combine(paths.BackupDirectory, $"{todayPrefix}-{DateTime.Now:HHmmss}.sqlite");
        CreateConsistentBackup(target);
        PruneAutomaticBackups(keep: 30);
        return target;
    }

    private string CreateBackupWithPrefix(string prefix)
    {
        EnsureDatabaseExists();
        var fileName = $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.sqlite";
        var target = Path.Combine(paths.BackupDirectory, fileName);
        CreateConsistentBackup(target);
        return target;
    }

    private void CreateConsistentBackup(string target)
    {
        var temporary = $"{target}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var source = OpenConnection(paths.DatabasePath, readOnly: false))
            using (var destination = OpenConnection(temporary, readOnly: false))
            {
                source.BackupDatabase(destination);
            }

            ValidateDatabase(temporary, requireApplicationSchema: true);
            File.Move(temporary, target, overwrite: false);
            OperationalLog.Write(paths, "DatabaseBackupCreated", properties: new Dictionary<string, object?>
            {
                ["backupFile"] = Path.GetFileName(target)
            });
        }
        catch
        {
            TryDelete(temporary);
            TryDelete(target);
            throw;
        }
    }

    private static void ValidateDatabase(string databasePath, bool requireApplicationSchema)
    {
        try
        {
            using var connection = OpenConnection(databasePath, readOnly: true);
            using (var integrity = connection.CreateCommand())
            {
                integrity.CommandText = "PRAGMA integrity_check;";
                var result = integrity.ExecuteScalar()?.ToString();
                if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"La verificación de integridad SQLite falló: {result ?? "sin resultado"}.");
                }
            }

            if (!requireApplicationSchema)
            {
                return;
            }

            using var schema = connection.CreateCommand();
            schema.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";
            using var reader = schema.ExecuteReader();
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }

            var missing = RequiredTables.Where(table => !tables.Contains(table)).ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidDataException($"El archivo no es un backup compatible. Faltan tablas: {string.Join(", ", missing)}.");
            }
        }
        catch (SqliteException exception)
        {
            throw new InvalidDataException("El archivo no es una base SQLite válida o está corrupto.", exception);
        }
    }

    private static SqliteConnection OpenConnection(string databasePath, bool readOnly)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        };
        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        return connection;
    }

    private void EnsureDatabaseExists()
    {
        if (!File.Exists(paths.DatabasePath))
        {
            throw new FileNotFoundException("No existe la base de datos para respaldar.", paths.DatabasePath);
        }
    }

    private void RestoreSafetyCopy(string safetyBackup)
    {
        var recoveryCandidate = $"{paths.DatabasePath}.recovery-{Guid.NewGuid():N}.tmp";
        try
        {
            File.Copy(safetyBackup, recoveryCandidate, overwrite: false);
            ValidateDatabase(recoveryCandidate, requireApplicationSchema: true);
            File.Replace(recoveryCandidate, paths.DatabasePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        finally
        {
            TryDelete(recoveryCandidate);
        }
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void PruneAutomaticBackups(int keep)
    {
        var files = Directory.EnumerateFiles(paths.BackupDirectory, "FPCalzados-auto-*.sqlite")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .Skip(keep);

        foreach (var file in files)
        {
            file.Delete();
        }
    }
}
