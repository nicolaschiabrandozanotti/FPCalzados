using FPCalzados.Domain;
using FPCalzados.Infrastructure;

namespace FPCalzados.Tests;

public sealed class ApplicationStoreTests
{
    [Fact]
    public void SaveProductStock_creates_multiple_color_size_rows_with_total_stock_from_variants()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new SystemClock());

        store.SaveProductStock(new ProductStockEditRequest(
            null,
            "Kioshi",
            "Roma",
            1,
            null,
            40000m,
            [
                new ProductVariantStockEditRequest(null, "Negro", "39", 2, null),
                new ProductVariantStockEditRequest(null, "Negro", "40", 3, null)
            ]));

        var rows = store.GetInventory("Roma");
        Assert.Equal(2, rows.Count);
        Assert.Equal(5, rows.Sum(item => item.StockQuantity));
        Assert.All(rows, item => Assert.Equal(0m, item.SupplierCost));
        Assert.All(rows, item => Assert.Equal(40000m, item.CashPrice));
    }

    [Fact]
    public void GetInventory_finds_products_by_supplier_name_mapped_to_brand()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new SystemClock());

        store.SaveSupplier(new SupplierEditRequest(null, "Distribuidora Norte", "Kioshi", true));
        store.SaveProductStock(new ProductStockEditRequest(
            null,
            "Kioshi",
            "Roma",
            1,
            null,
            40000m,
            [new ProductVariantStockEditRequest(null, "Negro", "39", 2, null)]));

        var rows = store.GetInventory("Norte");

        Assert.Single(rows);
        Assert.Equal("Kioshi", rows[0].Brand);
        Assert.Equal("Roma", rows[0].Model);
    }

    [Fact]
    public void SaveProductStock_deactivates_existing_variant_removed_from_request()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new SystemClock());

        store.SaveProductStock(new ProductStockEditRequest(
            null,
            "Kioshi",
            "Roma",
            1,
            null,
            40000m,
            [
                new ProductVariantStockEditRequest(null, "Negro", "39", 2, null),
                new ProductVariantStockEditRequest(null, "Negro", "40", 3, null)
            ]));
        var existingRows = store.GetInventory("Roma");
        var productId = existingRows[0].ProductId;
        var keptVariant = existingRows.Single(item => item.Size == "39");

        store.SaveProductStock(new ProductStockEditRequest(
            productId,
            "Kioshi",
            "Roma",
            1,
            null,
            40000m,
            [new ProductVariantStockEditRequest(keptVariant.VariantId, "Negro", "39", 2, null)]));

        Assert.Single(store.GetInventory("Roma"));
        var allRows = store.GetInventory("Roma", includeInactive: true);
        Assert.True(allRows.Single(item => item.Size == "39").IsActive);
        Assert.False(allRows.Single(item => item.Size == "40").IsActive);
    }

    [Fact]
    public void ImportStockRows_creates_product_variant_and_stock_movement()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new SystemClock());

        store.ImportStockRows([new StockImportRow("Kioshi", "Roma", "Negro", "39", 5, 20000m, 40000m, "ABC")]);

        using var db = factory.CreateDbContext();
        var variant = db.ProductVariants.Single();
        Assert.Equal(5, variant.StockQuantity);
        Assert.Equal("ABC", variant.Sku);
        Assert.Single(db.StockMovements);
    }

    [Fact]
    public void GetCustomerDebts_groups_pending_balances_by_customer()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        store.ImportStockRows([new StockImportRow("Kioshi", "Roma", "Negro", "39", 5, 20000m, 40000m, "NEG39")]);
        var row = store.GetInventory("NEG39").Single();
        var cashRule = store.GetPaymentRules().Single(item => item.PaymentMethod == PaymentMethod.Cash);
        var customer = new Customer { FirstName = "Ana", LastName = "Lopez", Phone = "123" };

        store.CreateSale(
            [new SaleLineDraft(row.VariantId, 1, 0m, cashRule.Id)],
            [new PaymentRequest(PaymentMethod.Cash, 10000m, 1)],
            customer,
            new DateOnly(2026, 5, 20));

        var debt = store.GetCustomerDebts().Single();
        Assert.Equal("Ana Lopez", debt.Customer);
        Assert.Equal("123", debt.Phone);
        Assert.Equal(1, debt.PendingSalesCount);
        Assert.Equal(30000m, debt.TotalPending);
        Assert.Equal(new DateOnly(2026, 5, 20), debt.NextDueDate);
        Assert.Equal("Pendiente", debt.Status);
    }

    [Fact]
    public void Sales_pagination_handles_thousands_of_rows_and_uses_date_index()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        using (var db = factory.CreateDbContext())
        {
            db.Sales.AddRange(Enumerable.Range(1, 1250).Select(index => new Sale
            {
                CreatedAt = new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero),
                CreatedDateKey = 20260517,
                Status = SaleStatus.Paid
            }));
            db.SaveChanges();
        }

        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));
        var first = store.GetSalesPage(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31), null, 1, 100);
        var last = store.GetSalesPage(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31), null, 13, 100);

        Assert.Equal(1250, first.TotalCount);
        Assert.Equal(13, first.TotalPages);
        Assert.Equal(100, first.Items.Count);
        Assert.Equal(50, last.Items.Count);
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={paths.DatabasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "EXPLAIN QUERY PLAN SELECT Id FROM Sales WHERE CreatedDateKey BETWEEN 20260501 AND 20260531;";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Contains("IX_Sales_CreatedDateKey", reader.GetString(3));
    }

    [Fact]
    public void Customers_with_different_documents_are_not_merged_when_they_share_phone()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));
        store.ImportStockRows([new StockImportRow("Marca", "Modelo", "Negro", "40", 2, 1m, 10m, "SHARED")]);
        var variant = store.GetInventory("SHARED").Single();
        var cashRule = store.GetPaymentRules().Single(rule => rule.PaymentMethod == PaymentMethod.Cash);

        store.CreateSale([new SaleLineDraft(variant.VariantId, 1, 0m, cashRule.Id)], [new PaymentRequest(PaymentMethod.Cash, 10m, 1)],
            new Customer { FirstName = "Ana", LastName = "Uno", Phone = "111", DocumentNumber = "DOC1" }, null);
        store.CreateSale([new SaleLineDraft(variant.VariantId, 1, 0m, cashRule.Id)], [new PaymentRequest(PaymentMethod.Cash, 10m, 1)],
            new Customer { FirstName = "Bea", LastName = "Dos", Phone = "111", DocumentNumber = "DOC2" }, null);

        using var db = factory.CreateDbContext();
        Assert.Equal(2, db.Customers.Count());
        Assert.Equal(2, db.Sales.Select(sale => sale.CustomerId).Distinct().Count());
    }

    [Fact]
    public void Admin_can_review_and_merge_duplicate_customer_records()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        int targetId;
        int sourceId;
        using (var db = factory.CreateDbContext())
        {
            var target = new Customer { FirstName = "Ana", LastName = "Duplicada", Phone = "123", NormalizedName = "ANA DUPLICADA", NormalizedPhone = "123" };
            var source = new Customer { FirstName = "Ana", LastName = "Duplicada", Phone = "123", NormalizedName = "ANA DUPLICADA", NormalizedPhone = "123" };
            db.Customers.AddRange(target, source);
            db.SaveChanges();
            targetId = target.Id;
            sourceId = source.Id;
            db.Sales.Add(new Sale { CustomerId = sourceId, Customer = source, CreatedAt = DateTimeOffset.Now, CreatedDateKey = 20260517 });
            db.SaveChanges();
        }

        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));
        Assert.Equal(2, store.GetPotentialCustomerDuplicates().Count);

        store.MergeCustomers(targetId, [sourceId]);

        using var merged = factory.CreateDbContext();
        Assert.Single(merged.Customers);
        Assert.Equal(targetId, merged.Sales.Single().CustomerId);
        Assert.Contains(merged.AuditLogs, log => log.Action == "Fusionar clientes");
    }

    [Fact]
    public void CreateSale_reuses_normalized_customer_and_history_uses_customer_id()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));
        store.ImportStockRows([new StockImportRow("Kioshi", "Roma", "Negro", "39", 5, 20000m, 40000m, "CLIENTE")]);
        var row = store.GetInventory("CLIENTE").Single();
        var cashRule = store.GetPaymentRules().Single(item => item.PaymentMethod == PaymentMethod.Cash);

        store.CreateSale(
            [new SaleLineDraft(row.VariantId, 1, 0m, cashRule.Id)],
            [new PaymentRequest(PaymentMethod.Cash, 40000m, 1)],
            new Customer { FirstName = "Ana", LastName = "López", Phone = "(11) 2233-4455", DocumentNumber = "12.345.678" }, null);
        store.CreateSale(
            [new SaleLineDraft(row.VariantId, 1, 0m, cashRule.Id)],
            [new PaymentRequest(PaymentMethod.Cash, 40000m, 1)],
            new Customer { FirstName = " ana ", LastName = "lopez", Phone = "1122334455", DocumentNumber = "12-345-678" }, null);

        using var db = factory.CreateDbContext();
        var persistedCustomer = Assert.Single(db.Customers);
        Assert.Equal("ANA LOPEZ", persistedCustomer.NormalizedName);
        Assert.Equal("1122334455", persistedCustomer.NormalizedPhone);
        Assert.Equal(2, db.Sales.Count(sale => sale.CustomerId == persistedCustomer.Id));
        var history = store.GetCustomerHistory("", "11 2233 4455");
        Assert.Equal(2, history.Sales.Count);
    }

    [Fact]
    public void GetSaleDetail_returns_items_payments_and_totals()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        store.ImportStockRows([new StockImportRow("Kioshi", "Roma", "Negro", "39", 5, 20000m, 40000m, "NEG39")]);
        var row = store.GetInventory("NEG39").Single();
        var cashRule = store.GetPaymentRules().Single(item => item.PaymentMethod == PaymentMethod.Cash);
        var sale = store.CreateSale(
            [new SaleLineDraft(row.VariantId, 1, 0m, cashRule.Id)],
            [new PaymentRequest(PaymentMethod.Cash, 15000m, 1)],
            new Customer { FirstName = "Ana", LastName = "Lopez", Phone = "123" },
            new DateOnly(2026, 5, 20));

        var detail = store.GetSaleDetail(sale.Id);

        Assert.Equal(sale.Id, detail.SaleId);
        Assert.Equal("Ana Lopez", detail.Customer);
        Assert.Equal("123", detail.Phone);
        Assert.Equal(40000m, detail.NetTotal);
        Assert.Equal(15000m, detail.PaidTotal);
        Assert.Equal(25000m, detail.BalanceDue);
        Assert.Single(detail.Items);
        Assert.Single(detail.Payments);
        Assert.Empty(detail.Returns);
    }

    [Fact]
    public void GetPaymentCollections_lists_payments_by_date_and_export_creates_file()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        store.ImportStockRows([new StockImportRow("Kioshi", "Roma", "Negro", "39", 5, 20000m, 40000m, "NEG39")]);
        var row = store.GetInventory("NEG39").Single();
        var cashRule = store.GetPaymentRules().Single(item => item.PaymentMethod == PaymentMethod.Cash);
        var sale = store.CreateSale(
            [new SaleLineDraft(row.VariantId, 1, 0m, cashRule.Id)],
            [new PaymentRequest(PaymentMethod.Cash, 10000m, 1)],
            new Customer { FirstName = "Ana", LastName = "Lopez", Phone = "123" },
            null);
        store.RegisterPayment(sale.Id, new PaymentRequest(PaymentMethod.Debit, 5000m, 1));

        var collections = store.GetPaymentCollections(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));
        var path = store.ExportPaymentCollectionsReport(paths.ExportDirectory, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

        Assert.Equal(2, collections.Count);
        Assert.Equal(15000m, collections.Sum(item => item.Amount));
        Assert.All(collections, item => Assert.Equal(sale.Id, item.SaleId));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void ExportSaleDetailReport_creates_comprobante_file()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        store.ImportStockRows([new StockImportRow("Kioshi", "Roma", "Negro", "39", 5, 20000m, 40000m, "NEG39")]);
        var row = store.GetInventory("NEG39").Single();
        var cashRule = store.GetPaymentRules().Single(item => item.PaymentMethod == PaymentMethod.Cash);
        var sale = store.CreateSale(
            [new SaleLineDraft(row.VariantId, 1, 0m, cashRule.Id)],
            [new PaymentRequest(PaymentMethod.Cash, 40000m, 1)],
            new Customer { FirstName = "Ana", LastName = "Lopez", Phone = "123" },
            null);

        var path = store.ExportSaleDetailReport(sale.Id, paths.ExportDirectory);

        Assert.True(File.Exists(path));
        Assert.Contains($"comprobante-venta-{sale.Id:000000}", Path.GetFileName(path));
    }

    [Fact]
    public void UpdateSaleDueDate_changes_debt_due_date_and_audits_action()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        store.ImportStockRows([new StockImportRow("Kioshi", "Roma", "Negro", "39", 5, 20000m, 40000m, "NEG39")]);
        var row = store.GetInventory("NEG39").Single();
        var cashRule = store.GetPaymentRules().Single(item => item.PaymentMethod == PaymentMethod.Cash);
        var sale = store.CreateSale(
            [new SaleLineDraft(row.VariantId, 1, 0m, cashRule.Id)],
            [new PaymentRequest(PaymentMethod.Cash, 10000m, 1)],
            new Customer { FirstName = "Ana", LastName = "Lopez", Phone = "123" },
            new DateOnly(2026, 5, 20));

        store.UpdateSaleDueDate(sale.Id, new DateOnly(2026, 6, 10));

        var updatedSale = store.GetSales(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)).Single();
        Assert.Equal(new DateOnly(2026, 6, 10), updatedSale.DueDate);
        Assert.Equal(new DateOnly(2026, 6, 10), store.GetCustomerDebts().Single().NextDueDate);
        Assert.Contains(store.GetAuditLogs(), item => item.Action == "Cambiar vencimiento" && item.EntityId == sale.Id.ToString());
    }

    [Fact]
    public void RegisterPayment_reduces_customer_debt_and_marks_sale_paid()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        store.ImportStockRows([new StockImportRow("Kioshi", "Roma", "Negro", "39", 5, 20000m, 40000m, "NEG39")]);
        var row = store.GetInventory("NEG39").Single();
        var cashRule = store.GetPaymentRules().Single(item => item.PaymentMethod == PaymentMethod.Cash);
        var sale = store.CreateSale(
            [new SaleLineDraft(row.VariantId, 1, 0m, cashRule.Id)],
            [new PaymentRequest(PaymentMethod.Cash, 10000m, 1)],
            new Customer { FirstName = "Ana", LastName = "Lopez", Phone = "123" },
            new DateOnly(2026, 5, 20));

        store.RegisterPayment(sale.Id, new PaymentRequest(PaymentMethod.Debit, 30000m, 1));

        Assert.Empty(store.GetCustomerDebts());
        var saleRow = store.GetSales(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)).Single();
        Assert.Equal(0m, saleRow.BalanceDue);
        Assert.Equal(40000m, saleRow.PaidTotal);
        Assert.Equal(SaleStatus.Paid, saleRow.Status);
        Assert.Contains(store.GetAuditLogs(), item => item.Action == "Registrar pago" && item.EntityId == sale.Id.ToString());
    }

    [Fact]
    public void Cash_register_cannot_be_closed_twice()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        store.OpenCashRegister(1000m, null);
        store.CloseCashRegister(1200m, null);

        Assert.Throws<InvalidOperationException>(() => store.CloseCashRegister(1300m, null));
        using var db = factory.CreateDbContext();
        Assert.Single(db.CashRegisterSessions);
        Assert.Single(db.AuditLogs.Where(log => log.Action == "CloseCashRegister"));
    }

    [Fact]
    public async Task Concurrent_full_payments_cannot_collect_the_same_balance_twice()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));
        store.ImportStockRows([new StockImportRow("Marca", "Modelo", "Negro", "40", 1, 1m, 40000m, "CONCURRENT")]);
        var variant = store.GetInventory("CONCURRENT").Single();
        var cashRule = store.GetPaymentRules().Single(rule => rule.PaymentMethod == PaymentMethod.Cash);
        var sale = store.CreateSale(
            [new SaleLineDraft(variant.VariantId, 1, 0m, cashRule.Id)],
            [],
            new Customer { FirstName = "Cliente", LastName = "Concurrente", Phone = "111" },
            null);
        using var gate = new ManualResetEventSlim(false);
        var attempts = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            gate.Wait();
            try
            {
                store.RegisterPayment(sale.Id, new PaymentRequest(PaymentMethod.Cash, 40000m, 1));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        })).ToArray();

        gate.Set();
        var results = await Task.WhenAll(attempts);

        Assert.Single(results, succeeded => succeeded);
        var detail = store.GetSaleDetail(sale.Id);
        Assert.Equal(40000m, detail.PaidTotal);
        Assert.Equal(0m, detail.BalanceDue);
        Assert.Single(detail.Payments);
    }

    [Fact]
    public void Product_and_audit_roll_back_together_when_unique_constraint_fails()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));
        store.SaveProductVariant(new ProductEditRequest(null, null, "Marca", "Uno", "Negro", "40", "DUP", 1, 0, 1m, 2m));
        using var beforeDb = factory.CreateDbContext();
        var auditCount = beforeDb.AuditLogs.Count();

        Assert.ThrowsAny<Exception>(() => store.SaveProductVariant(
            new ProductEditRequest(null, null, "Marca", "Dos", "Blanco", "41", "DUP", 1, 0, 1m, 2m)));

        using var afterDb = factory.CreateDbContext();
        Assert.Equal(auditCount, afterDb.AuditLogs.Count());
        Assert.Single(afterDb.ProductVariants);
    }

    [Fact]
    public void Admin_can_edit_and_delete_payment_rule_with_audit()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));
        var rule = store.GetPaymentRules().First();
        rule.Name = "Regla editada";
        store.SavePaymentRule(rule);
        Assert.Contains(store.GetPaymentRules(), item => item.Id == rule.Id && item.Name == "Regla editada");

        store.DeletePaymentRule(rule.Id);

        Assert.DoesNotContain(store.GetPaymentRules(), item => item.Id == rule.Id);
        using var db = factory.CreateDbContext();
        Assert.Contains(db.AuditLogs, log => log.Action == "Eliminar regla de pago" && log.EntityId == rule.Id.ToString());
    }

    [Fact]
    public void Seller_cannot_execute_administrative_operations()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));
        store.ChangeRolePassword(UserRole.Seller, "vendedor12345");
        store.Authenticate(UserRole.Seller, "vendedor12345");

        Assert.Throws<UnauthorizedAccessException>(() => store.SaveSupplier(new SupplierEditRequest(null, "No permitido", "Marca", true)));
        Assert.Throws<UnauthorizedAccessException>(() => store.ImportStockRows([new StockImportRow("Marca", "Modelo", "", "", 1, 1m, 2m, null)]));
        Assert.Throws<UnauthorizedAccessException>(() => store.GetAuditLogs());
        Assert.Throws<UnauthorizedAccessException>(() => store.ChangeRolePassword(UserRole.Admin, "intruso12345"));
    }

    [Fact]
    public void Admin_actions_are_recorded_in_audit_log()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        store.ChangeRolePassword(UserRole.Admin, "admin12345");
        store.Authenticate(UserRole.Admin, "admin12345");
        store.SaveSupplier(new SupplierEditRequest(null, "Distribuidora Norte", "Kioshi", true));

        var log = store.GetAuditLogs().Single(item => item.Action == "Guardar proveedor");
        Assert.Equal(UserRole.Admin, log.Role);
        Assert.Equal("Guardar proveedor", log.Action);
        Assert.Equal("Supplier", log.Entity);
        Assert.Contains("Distribuidora Norte", log.Message);
    }

    [Fact]
    public void GetDashboard_does_not_crash_on_empty_sqlite_sales_table()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        var summary = store.GetDashboard(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

        Assert.Equal(0m, summary.NetSales);
        Assert.Empty(summary.TopProducts);
    }

    [Fact]
    public void GetInventory_shows_price_override_for_variant()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new SystemClock());

        store.ImportStockRows([
            new StockImportRow("Kioshi", "Roma", "Negro", "39", 5, 20000m, 40000m, "NEG39"),
            new StockImportRow("Kioshi", "Roma", "Rojo", "39", 3, 20000m, 40000m, "ROJ39")
        ]);
        var productId = store.GetInventory("Roma").Single(item => item.Color == "Rojo").ProductId;

        store.SavePriceOverride(productId, "Rojo", "39", 45000m);

        var rows = store.GetInventory("Roma");
        Assert.Equal(40000m, rows.Single(item => item.Color == "Negro").CashPrice);
        Assert.Equal(45000m, rows.Single(item => item.Color == "Rojo").CashPrice);
    }

    [Theory]
    [InlineData("", "", "BASE")]
    [InlineData("Negro", "", "COLOR")]
    [InlineData("", "39", "TALLE")]
    public void SaveProductVariant_allows_optional_color_and_size(string color, string size, string sku)
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new SystemClock());

        store.SaveProductVariant(new ProductEditRequest(
            null,
            null,
            "Kioshi",
            $"Roma {sku}",
            color,
            size,
            sku,
            2,
            1,
            20000m,
            40000m));

        var row = store.GetInventory(sku).Single();
        Assert.Equal(color, row.Color);
        Assert.Equal(size, row.Size);
        Assert.Equal(2, row.StockQuantity);
    }

    [Fact]
    public void SaveProductVariant_keeps_separate_prices_for_color_and_size_options()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new SystemClock());

        store.SaveProductVariant(new ProductEditRequest(null, null, "Kioshi", "Roma", "", "", "BASE", 1, 1, 20000m, 40000m));
        var productId = store.GetInventory("BASE").Single().ProductId;
        store.SaveProductVariant(new ProductEditRequest(productId, null, "Kioshi", "Roma", "Rojo", "39", "ROJ39", 2, 1, 20000m, 45000m));

        var rows = store.GetInventory("Roma");
        Assert.Equal(40000m, rows.Single(item => item.Sku == "BASE").CashPrice);
        Assert.Equal(45000m, rows.Single(item => item.Sku == "ROJ39").CashPrice);
    }

    [Fact]
    public void SaveProductVariant_keeps_separate_supplier_cost_for_color_and_size_options()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new SystemClock());

        store.SaveProductVariant(new ProductEditRequest(null, null, "Kioshi", "Roma", "", "", "BASE", 1, 1, 20000m, 40000m));
        var productId = store.GetInventory("BASE").Single().ProductId;
        store.SaveProductVariant(new ProductEditRequest(productId, null, "Kioshi", "Roma", "Rojo", "39", "ROJ39", 2, 1, 25000m, 45000m));

        var rows = store.GetInventory("Roma");
        Assert.Equal(20000m, rows.Single(item => item.Sku == "BASE").SupplierCost);
        Assert.Equal(25000m, rows.Single(item => item.Sku == "ROJ39").SupplierCost);
    }

    [Fact]
    public void SaveProductVariant_applies_low_stock_threshold_to_all_product_options()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new SystemClock());

        store.SaveProductVariant(new ProductEditRequest(null, null, "Kioshi", "Roma", "Rojo", "39", "ROJ39", 2, 1, 20000m, 40000m));
        var productId = store.GetInventory("Roma").Single().ProductId;
        store.SaveProductVariant(new ProductEditRequest(productId, null, "Kioshi", "Roma", "Negro", "40", "NEG40", 3, 5, 20000m, 40000m));

        var rows = store.GetInventory("Roma");
        Assert.All(rows, item => Assert.Equal(5, item.LowStockThreshold));
    }

    [Fact]
    public void ImportStockRows_allows_optional_color_and_size()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new SystemClock());

        store.ImportStockRows([new StockImportRow("Kioshi", "Roma", "", "", 5, 20000m, 40000m, "BASE")]);

        var row = store.GetInventory("BASE").Single();
        Assert.Equal("", row.Color);
        Assert.Equal("", row.Size);
        Assert.Equal(5, row.StockQuantity);
    }

    [Fact]
    public void GetStockMovements_orders_latest_first_and_applies_take_limit()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        store.ImportStockRows([new StockImportRow("Kioshi", "Roma", "Negro", "39", 5, 20000m, 40000m, "NEG39")]);
        var row = store.GetInventory("NEG39").Single();
        store.SaveProductStock(new ProductStockEditRequest(
            row.ProductId,
            "Kioshi",
            "Roma",
            1,
            20000m,
            40000m,
            [new ProductVariantStockEditRequest(row.VariantId, "Negro", "39", 3, "NEG39")]));

        var movements = store.GetStockMovements(productId: row.ProductId, take: 1);

        var movement = Assert.Single(movements);
        Assert.Equal(StockMovementType.Adjustment, movement.Type);
        Assert.Equal(-2, movement.QuantityChange);
        Assert.Equal(3, movement.StockAfter);
    }

    [Fact]
    public void GetAuditLogs_orders_latest_first_and_applies_take_limit()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        store.ChangeRolePassword(UserRole.Admin, "admin12345");
        store.Authenticate(UserRole.Admin, "admin12345");
        store.SaveSupplier(new SupplierEditRequest(null, "Proveedor Uno", "Marca Uno", true));
        store.SaveSupplier(new SupplierEditRequest(null, "Proveedor Dos", "Marca Dos", true));

        var log = Assert.Single(store.GetAuditLogs(take: 1));
        Assert.Contains("Proveedor Dos", log.Message);
    }

    [Fact]
    public void Profitability_uses_supplier_cost_snapshot_after_cost_changes()
    {
        using var temp = new TempWorkspace();
        var paths = PortablePaths.FromRoot(temp.Root);
        var factory = new AppDbContextFactory(paths.DatabasePath);
        new DatabaseInitializer(factory, new AuthService()).Initialize();
        var store = new ApplicationStore(factory, new PricingService(), new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));

        store.ImportStockRows([new StockImportRow("Kioshi", "Roma", "Negro", "39", 5, 20000m, 40000m, "NEG39")]);
        var row = store.GetInventory("NEG39").Single();
        var cashRule = store.GetPaymentRules().Single(item => item.PaymentMethod == PaymentMethod.Cash);

        store.CreateSale(
            [new SaleLineDraft(row.VariantId, 1, 0m, cashRule.Id)],
            [new PaymentRequest(PaymentMethod.Cash, 40000m, 1)],
            null,
            null);

        using (var db = factory.CreateDbContext())
        {
            Assert.Equal(20000m, db.SaleItems.Single().SupplierCostSnapshot);
        }

        store.SaveProductStock(new ProductStockEditRequest(
            row.ProductId,
            "Kioshi",
            "Roma",
            1,
            30000m,
            40000m,
            [new ProductVariantStockEditRequest(row.VariantId, "Negro", "39", 4, "NEG39")]));

        var profitability = store.GetProfitability(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

        Assert.Equal(40000m, profitability.NetSales);
        Assert.Equal(20000m, profitability.EstimatedCost);
        Assert.Equal(20000m, profitability.GrossProfit);
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
