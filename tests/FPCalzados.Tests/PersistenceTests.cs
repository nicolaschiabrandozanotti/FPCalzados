using FPCalzados.Domain;
using FPCalzados.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FPCalzados.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public void DatabaseInitializer_creates_sqlite_database_with_default_roles_and_payment_rules()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        var initializer = new DatabaseInitializer(factory, new AuthService());

        initializer.Initialize();

        using var db = factory.CreateDbContext();
        Assert.True(File.Exists(paths.DatabasePath));
        Assert.Contains(db.RoleCredentials, credential => credential.Role == UserRole.Admin && credential.RequiresPasswordChange);
        Assert.Contains(db.RoleCredentials, credential => credential.Role == UserRole.Seller && credential.RequiresPasswordChange);
        Assert.Contains(db.PaymentRules, rule => rule.PaymentMethod == PaymentMethod.Credit && rule.Installments == 3);
        var credentialsFile = Path.Combine(temp.Root, "CREDENCIALES-INICIALES.txt");
        Assert.True(File.Exists(credentialsFile));
        Assert.DoesNotContain("admin\n", File.ReadAllText(credentialsFile), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BackupService_creates_consistent_backup_with_application_data()
    {
        using var temp = new TempWorkspace();
        var paths = InitializeDatabase(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        using (var db = factory.CreateDbContext())
        {
            db.Products.Add(new Product { Brand = "Marca", Model = "Modelo", IsActive = true });
            db.SaveChanges();
        }

        var backupPath = new BackupService(paths).CreateManualBackup();

        Assert.True(File.Exists(backupPath));
        Assert.StartsWith(paths.BackupDirectory, backupPath, StringComparison.OrdinalIgnoreCase);
        using var backupDb = new AppDbContextFactory(backupPath).CreateDbContext();
        Assert.Contains(backupDb.Products, product => product.Brand == "Marca" && product.Model == "Modelo");
    }

    [Fact]
    public void BackupService_includes_committed_wal_data_while_connection_remains_open()
    {
        using var temp = new TempWorkspace();
        var paths = InitializeDatabase(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        using var active = factory.CreateDbContext();
        active.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        active.Products.Add(new Product { Brand = "WAL", Model = "Pendiente de checkpoint", IsActive = true });
        active.SaveChanges();

        var backupPath = new BackupService(paths).CreateManualBackup();

        using var backup = new AppDbContextFactory(backupPath).CreateDbContext();
        Assert.Contains(backup.Products, product => product.Brand == "WAL");
    }

    [Fact]
    public void BackupService_rejects_corrupt_restore_without_modifying_active_database()
    {
        using var temp = new TempWorkspace();
        var paths = InitializeDatabase(temp.Root);
        var originalBytes = File.ReadAllBytes(paths.DatabasePath);
        var corruptBackup = Path.Combine(temp.Root, "corrupt.sqlite");
        File.WriteAllText(corruptBackup, "not a sqlite database");

        Assert.Throws<InvalidDataException>(() => new BackupService(paths).RestoreBackup(corruptBackup));

        Assert.Equal(originalBytes, File.ReadAllBytes(paths.DatabasePath));
    }

    [Fact]
    public void BackupService_rejects_restore_with_wrong_extension()
    {
        using var temp = new TempWorkspace();
        var paths = InitializeDatabase(temp.Root);
        var invalidPath = Path.Combine(temp.Root, "backup.txt");
        File.WriteAllText(invalidPath, "not a backup");

        var exception = Assert.Throws<InvalidOperationException>(() => new BackupService(paths).RestoreBackup(invalidPath));

        Assert.Contains(".sqlite", exception.Message);
    }

    [Fact]
    public void BackupService_rejects_valid_sqlite_without_application_schema()
    {
        using var temp = new TempWorkspace();
        var paths = InitializeDatabase(temp.Root);
        var unrelatedDatabase = Path.Combine(temp.Root, "unrelated.sqlite");
        using (var db = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={unrelatedDatabase};Pooling=False"))
        {
            db.Open();
            using var command = db.CreateCommand();
            command.CommandText = "CREATE TABLE Unrelated (Id INTEGER PRIMARY KEY);";
            command.ExecuteNonQuery();
        }

        var exception = Assert.Throws<InvalidDataException>(() => new BackupService(paths).RestoreBackup(unrelatedDatabase));

        Assert.Contains("Faltan tablas", exception.Message);
    }

    [Fact]
    public void BackupService_recovers_safety_copy_when_failure_occurs_after_replace()
    {
        using var temp = new TempWorkspace();
        var paths = InitializeDatabase(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        using (var db = factory.CreateDbContext())
        {
            db.Products.Add(new Product { Brand = "Backup", Model = "Anterior", IsActive = true });
            db.SaveChanges();
        }

        var validBackup = new BackupService(paths).CreateManualBackup();
        using (var db = factory.CreateDbContext())
        {
            db.Products.Add(new Product { Brand = "Estado", Model = "A recuperar", IsActive = true });
            db.SaveChanges();
        }

        var serviceWithFailure = new BackupService(paths, () => throw new IOException("Fallo inyectado"));
        Assert.Throws<IOException>(() => serviceWithFailure.RestoreBackup(validBackup));

        using var recovered = factory.CreateDbContext();
        Assert.Contains(recovered.Products, product => product.Brand == "Estado");
        Assert.Contains(recovered.Products, product => product.Brand == "Backup");
    }

    [Fact]
    public void BackupService_restores_valid_backup_and_creates_safety_copy()
    {
        using var temp = new TempWorkspace();
        var paths = InitializeDatabase(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        using (var db = factory.CreateDbContext())
        {
            db.Products.Add(new Product { Brand = "Antes", Model = "Original", IsActive = true });
            db.SaveChanges();
        }

        var service = new BackupService(paths);
        var backupPath = service.CreateManualBackup();
        using (var db = factory.CreateDbContext())
        {
            db.Products.Add(new Product { Brand = "Después", Model = "Cambio", IsActive = true });
            db.SaveChanges();
        }

        var result = service.RestoreBackup(backupPath);

        Assert.True(File.Exists(result.SafetyBackupPath));
        using var restoredDb = factory.CreateDbContext();
        Assert.Contains(restoredDb.Products, product => product.Brand == "Antes");
        Assert.DoesNotContain(restoredDb.Products, product => product.Brand == "Después");
        using var safetyDb = new AppDbContextFactory(result.SafetyBackupPath).CreateDbContext();
        Assert.Contains(safetyDb.Products, product => product.Brand == "Después");
    }

    [Fact]
    public void DatabaseInitializer_applies_migrations_and_database_constraints()
    {
        using var temp = new TempWorkspace();
        var paths = InitializeDatabase(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        using var db = factory.CreateDbContext();

        Assert.Equal(4, db.Database.GetAppliedMigrations().Count());
        db.Products.Add(new Product { Brand = "Inválido", Model = "Precio", SupplierCost = -1m, CashPrice = 10m });
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void DatabaseInitializer_upgrades_legacy_database_without_migration_history()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        Directory.CreateDirectory(paths.DataDirectory);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        using (var legacy = factory.CreateDbContext())
        {
            legacy.GetService<IMigrator>().Migrate("20260713003555_InitialSchema");
            legacy.Products.Add(new Product { Brand = "Legacy", Model = "Sin historial", SupplierCost = 1m, CashPrice = 2m });
            legacy.SaveChanges();
            legacy.Database.ExecuteSqlRaw("DROP TABLE __EFMigrationsHistory;");
        }

        new DatabaseInitializer(factory, new AuthService()).Initialize();

        using var upgraded = factory.CreateDbContext();
        Assert.Equal(4, upgraded.Database.GetAppliedMigrations().Count());
        Assert.Contains(upgraded.Products, product => product.Brand == "Legacy");
        Assert.NotEmpty(Directory.EnumerateFiles(paths.BackupDirectory, "FPCalzados-backup-*.sqlite"));
    }

    [Fact]
    public void DatabaseInitializer_upgrades_previous_schema_with_backup_and_preserves_data()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        Directory.CreateDirectory(paths.DataDirectory);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        using (var oldDb = factory.CreateDbContext())
        {
            oldDb.GetService<IMigrator>().Migrate("20260713003555_InitialSchema");
            oldDb.Products.Add(new Product { Brand = "Conservada", Model = "Migración", SupplierCost = 1m, CashPrice = 2m });
            oldDb.SaveChanges();
        }

        new DatabaseInitializer(factory, new AuthService()).Initialize();

        using var upgraded = factory.CreateDbContext();
        Assert.Equal(4, upgraded.Database.GetAppliedMigrations().Count());
        Assert.Contains(upgraded.Products, product => product.Brand == "Conservada");
        Assert.NotEmpty(Directory.EnumerateFiles(paths.BackupDirectory, "FPCalzados-backup-*.sqlite"));
    }

    [Fact]
    public void DatabaseInitializer_reports_duplicates_before_unique_index_migration()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        Directory.CreateDirectory(paths.DataDirectory);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        using (var oldDb = factory.CreateDbContext())
        {
            oldDb.GetService<IMigrator>().Migrate("20260713003555_InitialSchema");
            var product = new Product { Brand = "Duplicada", Model = "Variante", SupplierCost = 1m, CashPrice = 2m };
            product.Variants.Add(new ProductVariant { Color = "Negro", Size = "40", StockQuantity = 1 });
            product.Variants.Add(new ProductVariant { Color = "Negro", Size = "40", StockQuantity = 1 });
            oldDb.Products.Add(product);
            oldDb.SaveChanges();
        }

        var exception = Assert.Throws<InvalidDataException>(() => new DatabaseInitializer(factory, new AuthService()).Initialize());

        Assert.Contains("variantes repetidas", exception.Message);
        using var preserved = factory.CreateDbContext();
        Assert.Equal(2, preserved.ProductVariants.Count());
        Assert.Single(preserved.Database.GetAppliedMigrations());
    }

    [Fact]
    public void DatabaseInitializer_restores_backup_when_migration_fails()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        Directory.CreateDirectory(paths.DataDirectory);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        using (var oldDb = factory.CreateDbContext())
        {
            oldDb.GetService<IMigrator>().Migrate("20260713003555_InitialSchema");
            oldDb.Products.Add(new Product { Brand = "Antes", Model = "Del fallo", SupplierCost = 1m, CashPrice = 2m });
            oldDb.SaveChanges();
        }

        var initializer = new DatabaseInitializer(factory, new AuthService(), () => throw new InvalidOperationException("Fallo de migración inyectado"));
        Assert.Throws<InvalidOperationException>(() => initializer.Initialize());

        using var restored = factory.CreateDbContext();
        Assert.Single(restored.Database.GetAppliedMigrations());
        Assert.Contains(restored.Products, product => product.Brand == "Antes");
    }

    [Fact]
    public void ProductVariant_concurrency_token_rejects_stale_stock_update()
    {
        using var temp = new TempWorkspace();
        var paths = InitializeDatabase(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        using (var setup = factory.CreateDbContext())
        {
            setup.ProductVariants.Add(new ProductVariant
            {
                Product = new Product { Brand = "Concurrente", Model = "Stock", IsActive = true },
                Color = "Negro",
                Size = "40",
                StockQuantity = 2
            });
            setup.SaveChanges();
        }

        using var first = factory.CreateDbContext();
        using var second = factory.CreateDbContext();
        var firstVariant = first.ProductVariants.Single();
        var staleVariant = second.ProductVariants.Single();
        firstVariant.StockQuantity--;
        firstVariant.Version++;
        first.SaveChanges();
        staleVariant.StockQuantity--;
        staleVariant.Version++;

        Assert.Throws<DbUpdateConcurrencyException>(() => second.SaveChanges());
    }

    private static PortablePaths InitializeDatabase(string root)
    {
        var paths = PortablePaths.FromRoot(root);
        new DatabaseInitializer(new AppDbContextFactory(paths.DatabasePath), new AuthService()).Initialize();
        return paths;
    }

    private sealed class TempWorkspace : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "fpcalzados-tests", Guid.NewGuid().ToString("N"));

        public TempWorkspace()
        {
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
