using FPCalzados.Domain;
using Microsoft.EntityFrameworkCore;

namespace FPCalzados.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<PriceOverride> PriceOverrides => Set<PriceOverride>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ReturnRecord> Returns => Set<ReturnRecord>();
    public DbSet<CashRegisterSession> CashRegisterSessions => Set<CashRegisterSession>();
    public DbSet<PaymentRule> PaymentRules => Set<PaymentRule>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<RoleCredential> RoleCredentials => Set<RoleCredential>();
    public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureProducts(modelBuilder);
        ConfigureSales(modelBuilder);
        ConfigureSettings(modelBuilder);
    }

    private static void ConfigureProducts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(item => new { item.Brand, item.Model }).IsUnique();
            entity.Property(item => item.Brand).HasMaxLength(120);
            entity.Property(item => item.Model).HasMaxLength(160);
            entity.Property(item => item.SupplierCost).HasPrecision(18, 2);
            entity.Property(item => item.CashPrice).HasPrecision(18, 2);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Products_SupplierCost_NonNegative", "CAST(SupplierCost AS REAL) >= 0");
                table.HasCheckConstraint("CK_Products_CashPrice_NonNegative", "CAST(CashPrice AS REAL) >= 0");
            });
            entity.Ignore(item => item.DisplayName);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(item => new { item.Name, item.Brand }).IsUnique();
            entity.Property(item => item.Name).HasMaxLength(160);
            entity.Property(item => item.Brand).HasMaxLength(120);
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasIndex(item => item.Sku).IsUnique();
            entity.HasIndex(item => new { item.ProductId, item.Color, item.Size }).IsUnique();
            entity.Property(item => item.Color).HasMaxLength(80);
            entity.Property(item => item.Size).HasMaxLength(20);
            entity.Property(item => item.Sku).HasMaxLength(80);
            entity.Property(item => item.SupplierCostOverride).HasPrecision(18, 2);
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_ProductVariants_Stock_NonNegative", "StockQuantity >= 0");
                table.HasCheckConstraint("CK_ProductVariants_Threshold_NonNegative", "LowStockThreshold >= 0");
                table.HasCheckConstraint("CK_ProductVariants_Cost_NonNegative", "SupplierCostOverride IS NULL OR CAST(SupplierCostOverride AS REAL) >= 0");
            });
            entity.Ignore(item => item.DisplayName);
            entity.HasOne(item => item.Product)
                .WithMany(item => item.Variants)
                .HasForeignKey(item => item.ProductId);
        });

        modelBuilder.Entity<PriceOverride>(entity =>
        {
            entity.Property(item => item.Color).HasMaxLength(80);
            entity.Property(item => item.Size).HasMaxLength(20);
            entity.Property(item => item.Price).HasPrecision(18, 2);
            entity.HasIndex(item => new { item.ProductId, item.Color, item.Size }).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint("CK_PriceOverrides_Price_NonNegative", "CAST(Price AS REAL) >= 0"));
            entity.HasOne(item => item.Product)
                .WithMany(item => item.PriceOverrides)
                .HasForeignKey(item => item.ProductId);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.Property(item => item.Reason).HasMaxLength(240);
            entity.HasOne(item => item.Variant)
                .WithMany(item => item.Movements)
                .HasForeignKey(item => item.ProductVariantId);
        });
    }

    private static void ConfigureSales(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(item => item.FirstName).HasMaxLength(120);
            entity.Property(item => item.LastName).HasMaxLength(120);
            entity.Property(item => item.Phone).HasMaxLength(80);
            entity.Property(item => item.DocumentNumber).HasMaxLength(80);
            entity.Property(item => item.Address).HasMaxLength(240);
            entity.Property(item => item.Notes).HasMaxLength(500);
            entity.Property(item => item.NormalizedName).HasMaxLength(241);
            entity.Property(item => item.NormalizedPhone).HasMaxLength(80);
            entity.Property(item => item.NormalizedDocument).HasMaxLength(80);
            entity.HasIndex(item => item.NormalizedDocument).IsUnique().HasFilter("NormalizedDocument IS NOT NULL");
            entity.HasIndex(item => item.NormalizedPhone);
            entity.HasIndex(item => item.NormalizedName);
            entity.Ignore(item => item.FullName);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Ignore(item => item.GrossTotal);
            entity.Ignore(item => item.ReturnedTotal);
            entity.Ignore(item => item.NetTotal);
            entity.Ignore(item => item.PaidTotal);
            entity.Ignore(item => item.BalanceDue);
            entity.Ignore(item => item.CashToReturn);
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasIndex(item => item.CreatedDateKey);
            entity.HasOne(item => item.Customer).WithMany().HasForeignKey(item => item.CustomerId);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.Property(item => item.ManualDiscount).HasPrecision(18, 2);
            entity.Property(item => item.FinalUnitPrice).HasPrecision(18, 2);
            entity.Property(item => item.SupplierCostSnapshot).HasPrecision(18, 2);
            entity.Property(item => item.PercentModifierSnapshot).HasPrecision(9, 2);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_SaleItems_Quantity_Positive", "Quantity > 0");
                table.HasCheckConstraint("CK_SaleItems_ReturnedQuantity_Valid", "ReturnedQuantity >= 0 AND ReturnedQuantity <= Quantity");
                table.HasCheckConstraint("CK_SaleItems_Prices_NonNegative", "CAST(UnitPrice AS REAL) >= 0 AND CAST(ManualDiscount AS REAL) >= 0 AND CAST(FinalUnitPrice AS REAL) >= 0");
            });
            entity.HasOne(item => item.Variant).WithMany();
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(item => item.Amount).HasPrecision(18, 2);
            entity.HasIndex(item => item.PaidDateKey);
            entity.ToTable(table => table.HasCheckConstraint("CK_Payments_Amount_Positive", "CAST(Amount AS REAL) > 0"));
        });

        modelBuilder.Entity<ReturnRecord>(entity =>
        {
            entity.Property(item => item.Amount).HasPrecision(18, 2);
            entity.Property(item => item.CashToReturn).HasPrecision(18, 2);
            entity.Property(item => item.Reason).HasMaxLength(240);
        });

        modelBuilder.Entity<CashRegisterSession>(entity =>
        {
            entity.Property(item => item.OpeningCash).HasPrecision(18, 2);
            entity.Property(item => item.ClosingCash).HasPrecision(18, 2);
            entity.Property(item => item.Notes).HasMaxLength(500);
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.Ignore(item => item.IsOpen);
            entity.HasIndex(item => item.OpenedAt);
            entity.HasIndex(item => item.ClosedAt);
        });
    }

    private static void ConfigureSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentRule>(entity =>
        {
            entity.Property(item => item.Name).HasMaxLength(120);
            entity.Property(item => item.PercentModifier).HasPrecision(9, 2);
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.Property(item => item.Name).HasMaxLength(120);
            entity.Property(item => item.Brand).HasMaxLength(120);
            entity.Property(item => item.Model).HasMaxLength(160);
            entity.Property(item => item.PercentDiscount).HasPrecision(9, 2);
        });

        modelBuilder.Entity<RoleCredential>(entity =>
        {
            entity.HasIndex(item => item.Role).IsUnique();
            entity.Property(item => item.PasswordHash).HasMaxLength(300);
        });

        modelBuilder.Entity<BusinessSettings>(entity =>
        {
            entity.Property(item => item.Name).HasMaxLength(160);
            entity.Property(item => item.Address).HasMaxLength(240);
            entity.Property(item => item.Phone).HasMaxLength(80);
            entity.Property(item => item.TaxId).HasMaxLength(80);
            entity.Property(item => item.TicketFooter).HasMaxLength(240);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(item => item.Action).HasMaxLength(80);
            entity.Property(item => item.Entity).HasMaxLength(80);
            entity.Property(item => item.EntityId).HasMaxLength(80);
            entity.Property(item => item.Message).HasMaxLength(500);
        });
    }
}
