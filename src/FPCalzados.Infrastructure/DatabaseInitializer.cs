using FPCalzados.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FPCalzados.Infrastructure;

public sealed class DatabaseInitializer
{
    private readonly AppDbContextFactory factory;
    private readonly AuthService authService;
    private readonly Action? beforeMigrate;

    public DatabaseInitializer(AppDbContextFactory factory, AuthService authService)
        : this(factory, authService, null)
    {
    }

    internal DatabaseInitializer(AppDbContextFactory factory, AuthService authService, Action? beforeMigrate)
    {
        this.factory = factory;
        this.authService = authService;
        this.beforeMigrate = beforeMigrate;
    }

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(factory.DatabasePath)!);
        var databaseExisted = File.Exists(factory.DatabasePath);
        string? preMigrationBackup = null;
        try
        {
            using var migrationDb = factory.CreateDbContext();
            var legacyDatabase = HasTable(migrationDb, "Products") && !HasTable(migrationDb, "__EFMigrationsHistory");
            if (legacyDatabase)
            {
                preMigrationBackup = CreatePreMigrationBackup();
                EnsureSchema(migrationDb);
                BaselineLegacyDatabase(migrationDb);
            }
            else if (databaseExisted && HasPendingMigrations(migrationDb))
            {
                preMigrationBackup = CreatePreMigrationBackup();
            }

            ValidateMigrationData(migrationDb);
            beforeMigrate?.Invoke();
            migrationDb.Database.Migrate();
            OperationalLog.Write(GetPortablePaths(), "DatabaseMigrationCompleted", properties: new Dictionary<string, object?>
            {
                ["appliedMigrations"] = migrationDb.Database.GetAppliedMigrations().Count()
            });
        }
        catch (Exception exception)
        {
            OperationalLog.Write(GetPortablePaths(), "DatabaseMigrationFailed", "Error", new Dictionary<string, object?>
            {
                ["exceptionType"] = exception.GetType().FullName,
                ["recoveryAvailable"] = preMigrationBackup is not null
            });
            if (preMigrationBackup is not null)
            {
                RestorePreMigrationBackup(preMigrationBackup);
            }

            throw;
        }

        using var db = factory.CreateDbContext();

        if (!db.RoleCredentials.Any())
        {
            var adminPassword = AuthService.GenerateTemporaryPassword();
            var sellerPassword = AuthService.GenerateTemporaryPassword();
            db.RoleCredentials.Add(authService.CreateCredential(UserRole.Admin, adminPassword, requiresPasswordChange: true));
            db.RoleCredentials.Add(authService.CreateCredential(UserRole.Seller, sellerPassword, requiresPasswordChange: true));
            WriteInitialCredentials(adminPassword, sellerPassword);
        }

        if (!db.BusinessSettings.Any())
        {
            db.BusinessSettings.Add(new BusinessSettings());
        }

        if (!db.PaymentRules.Any())
        {
            db.PaymentRules.AddRange(
                new PaymentRule
                {
                    Name = "Efectivo",
                    PaymentMethod = PaymentMethod.Cash,
                    PercentModifier = 0m,
                    Installments = 1
                },
                new PaymentRule
                {
                    Name = "Debito",
                    PaymentMethod = PaymentMethod.Debit,
                    PercentModifier = 0m,
                    Installments = 1
                },
                new PaymentRule
                {
                    Name = "Credito 1 cuota",
                    PaymentMethod = PaymentMethod.Credit,
                    PercentModifier = 10m,
                    Installments = 1
                },
                new PaymentRule
                {
                    Name = "Credito 3 cuotas",
                    PaymentMethod = PaymentMethod.Credit,
                    PercentModifier = 15m,
                    Installments = 3
                },
                new PaymentRule
                {
                    Name = "Credito 6 cuotas",
                    PaymentMethod = PaymentMethod.Credit,
                    PercentModifier = 25m,
                    Installments = 6
                });
        }

        db.SaveChanges();
    }

    private string CreatePreMigrationBackup()
    {
        return new BackupService(GetPortablePaths()).CreateManualBackup();
    }

    private void RestorePreMigrationBackup(string backupPath)
    {
        new BackupService(GetPortablePaths()).RestoreBackup(backupPath);
    }

    private PortablePaths GetPortablePaths()
    {
        var dataDirectory = Path.GetDirectoryName(factory.DatabasePath)!;
        var rootDirectory = Directory.GetParent(dataDirectory)?.FullName ?? dataDirectory;
        return PortablePaths.FromRoot(rootDirectory);
    }

    private static void ValidateMigrationData(AppDbContext db)
    {
        if (!HasTable(db, "ProductVariants") || !HasTable(db, "PriceOverrides"))
        {
            return;
        }

        var problems = new List<string>();
        if (HasDuplicate(db, "SELECT COUNT(*) FROM (SELECT ProductId, Color, Size FROM ProductVariants GROUP BY ProductId, Color, Size HAVING COUNT(*) > 1);"))
        {
            problems.Add("variantes repetidas para el mismo producto, color y talle");
        }

        if (HasDuplicate(db, "SELECT COUNT(*) FROM (SELECT Sku FROM ProductVariants WHERE Sku IS NOT NULL GROUP BY Sku HAVING COUNT(*) > 1);"))
        {
            problems.Add("códigos SKU repetidos");
        }

        if (HasDuplicate(db, "SELECT COUNT(*) FROM (SELECT ProductId, Color, Size FROM PriceOverrides GROUP BY ProductId, Color, Size HAVING COUNT(*) > 1);"))
        {
            problems.Add("precios especiales repetidos");
        }

        if (problems.Count > 0)
        {
            throw new InvalidDataException($"La migración no puede continuar porque existen {string.Join(", ", problems)}. Corrige los duplicados o solicita asistencia; la base original fue preservada.");
        }
    }

    private static bool HasDuplicate(AppDbContext db, string sql)
    {
        using var connection = (SqliteConnection)db.Database.GetDbConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static bool HasPendingMigrations(AppDbContext db)
    {
        return db.Database.GetPendingMigrations().Any();
    }

    private static bool HasTable(AppDbContext db, string table)
    {
        using var connection = (SqliteConnection)db.Database.GetDbConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $table;";
        command.Parameters.AddWithValue("$table", table);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void BaselineLegacyDatabase(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                ProductVersion TEXT NOT NULL
            );
            INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion)
            VALUES ('20260713003555_InitialSchema', '10.0.8');
            """);
    }

    private static void EnsureSchema(AppDbContext db)
    {
        using var connection = (SqliteConnection)db.Database.GetDbConnection();
        connection.Open();
        EnsureSuppliersTable(connection);
        EnsureSupplierCostOverrideColumn(connection);
        EnsureSupplierCostSnapshotColumn(connection);
        EnsureCashRegisterSessionsTable(connection);
        EnsureBusinessSettingsTable(connection);
        EnsureAuditLogsTable(connection);
        EnsureAuthenticationColumns(connection);
        EnsureConcurrencyColumns(connection);
    }

    private static void EnsureConcurrencyColumns(SqliteConnection connection)
    {
        EnsureColumn(connection, "ProductVariants", "Version", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Sales", "Version", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "CashRegisterSessions", "Version", "INTEGER NOT NULL DEFAULT 0");
    }

    private void WriteInitialCredentials(string adminPassword, string sellerPassword)
    {
        var dataDirectory = Path.GetDirectoryName(factory.DatabasePath)!;
        var rootDirectory = Directory.GetParent(dataDirectory)?.FullName ?? dataDirectory;
        var credentialsPath = Path.Combine(rootDirectory, "CREDENCIALES-INICIALES.txt");
        File.WriteAllText(credentialsPath, $"""
            FPCalzados - credenciales temporales

            Administrador: {adminPassword}
            Vendedor: {sellerPassword}

            La aplicación exigirá cambiar cada contraseña en el primer ingreso.
            Elimina este archivo cuando ambos roles hayan actualizado su contraseña.
            """);
    }

    private static void EnsureAuthenticationColumns(SqliteConnection connection)
    {
        EnsureColumn(connection, "RoleCredentials", "RequiresPasswordChange", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "RoleCredentials", "FailedLoginAttempts", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "RoleCredentials", "LockedUntil", "TEXT NULL");
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string definition)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({table});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        alter.ExecuteNonQuery();
    }

    private static void EnsureSuppliersTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Suppliers (
                Id INTEGER NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Brand TEXT NOT NULL,
                IsActive INTEGER NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Suppliers_Name_Brand ON Suppliers (Name, Brand);
            """;
        command.ExecuteNonQuery();
    }

    private static void EnsureCashRegisterSessionsTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS CashRegisterSessions (
                Id INTEGER NOT NULL CONSTRAINT PK_CashRegisterSessions PRIMARY KEY AUTOINCREMENT,
                OpenedAt TEXT NOT NULL,
                ClosedAt TEXT NULL,
                OpeningCash TEXT NOT NULL,
                ClosingCash TEXT NULL,
                Notes TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_CashRegisterSessions_OpenedAt ON CashRegisterSessions (OpenedAt);
            CREATE INDEX IF NOT EXISTS IX_CashRegisterSessions_ClosedAt ON CashRegisterSessions (ClosedAt);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_CashRegisterSessions_OnlyOneOpen ON CashRegisterSessions ((1)) WHERE ClosedAt IS NULL;
            """;
        command.ExecuteNonQuery();
    }

    private static void EnsureBusinessSettingsTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS BusinessSettings (
                Id INTEGER NOT NULL CONSTRAINT PK_BusinessSettings PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Address TEXT NOT NULL,
                Phone TEXT NOT NULL,
                TaxId TEXT NOT NULL,
                TicketFooter TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private static void EnsureAuditLogsTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS AuditLogs (
                Id INTEGER NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY AUTOINCREMENT,
                OccurredAt TEXT NOT NULL,
                Role INTEGER NULL,
                Action TEXT NOT NULL,
                Entity TEXT NOT NULL,
                EntityId TEXT NULL,
                Message TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_AuditLogs_OccurredAt ON AuditLogs (OccurredAt);
            """;
        command.ExecuteNonQuery();
    }

    private static void EnsureSupplierCostOverrideColumn(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(ProductVariants);";
        using var reader = command.ExecuteReader();
        var hasSupplierCostOverride = false;
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), "SupplierCostOverride", StringComparison.OrdinalIgnoreCase))
            {
                hasSupplierCostOverride = true;
                break;
            }
        }

        if (hasSupplierCostOverride)
        {
            return;
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = "ALTER TABLE ProductVariants ADD COLUMN SupplierCostOverride TEXT NULL;";
        alter.ExecuteNonQuery();
    }

    private static void EnsureSupplierCostSnapshotColumn(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(SaleItems);";
        using var reader = command.ExecuteReader();
        var hasSupplierCostSnapshot = false;
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), "SupplierCostSnapshot", StringComparison.OrdinalIgnoreCase))
            {
                hasSupplierCostSnapshot = true;
                break;
            }
        }

        if (hasSupplierCostSnapshot)
        {
            return;
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = "ALTER TABLE SaleItems ADD COLUMN SupplierCostSnapshot TEXT NOT NULL DEFAULT 0;";
        alter.ExecuteNonQuery();
    }
}
