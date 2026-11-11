using System.Text;
using ClosedXML.Excel;
using FPCalzados.Domain;
using Microsoft.EntityFrameworkCore;

namespace FPCalzados.Infrastructure;

internal sealed class SalesApplicationService(
    AppDbContextFactory factory,
    PricingService pricingService,
    IClock clock)
{
    public Sale CreateSale(
        IReadOnlyList<SaleLineDraft> lines,
        IReadOnlyList<PaymentRequest> payments,
        Customer? customer,
        DateOnly? dueDate,
        UserRole? role)
    {
        using var db = factory.CreateDbContext();
        using var transaction = db.Database.BeginTransaction();
        var paymentRules = db.PaymentRules.ToDictionary(item => item.Id);
        var itemRequests = new List<SaleItemRequest>();

        foreach (var line in lines)
        {
            var variant = db.ProductVariants
                .Include(item => item.Product)
                .ThenInclude(item => item.PriceOverrides)
                .Include(item => item.Movements)
                .Single(item => item.Id == line.VariantId);

            itemRequests.Add(new SaleItemRequest
            {
                Variant = variant,
                Quantity = line.Quantity,
                ManualDiscount = line.ManualDiscount,
                PaymentRule = paymentRules[line.PaymentRuleId]
            });
        }

        var persistedCustomer = ResolveCustomer(db, customer);
        var sale = new SalesService(pricingService, clock).CreateSale(new CreateSaleRequest
        {
            Items = itemRequests,
            Payments = payments,
            Customer = persistedCustomer,
            DueDate = dueDate
        });

        foreach (var item in sale.Items)
        {
            item.Id = 0;
            item.Variant.Version++;
        }

        db.Sales.Add(sale);
        db.SaveChanges();
        AuditWriter.Add(db, clock, role, "Crear venta", "Sale", sale.Id.ToString(), $"Venta creada. Neto: {sale.NetTotal}. Saldo: {sale.BalanceDue}.");
        db.SaveChanges();
        transaction.Commit();
        return sale;
    }

    private static Customer? ResolveCustomer(AppDbContext db, Customer? input)
    {
        if (input is null)
        {
            return null;
        }

        var normalizedName = DataNormalizer.Text($"{input.FirstName} {input.LastName}");
        var normalizedPhone = DataNormalizer.Phone(input.Phone);
        var normalizedDocument = DataNormalizer.Document(input.DocumentNumber);
        if (normalizedName.Length == 0)
        {
            throw new InvalidOperationException("El nombre del cliente es obligatorio.");
        }

        Customer? customer = null;
        if (normalizedDocument is not null)
        {
            customer = db.Customers.SingleOrDefault(item => item.NormalizedDocument == normalizedDocument);
        }

        if (customer is null && normalizedDocument is null && normalizedPhone.Length > 0)
        {
            var phoneMatches = db.Customers.Where(item => item.NormalizedPhone == normalizedPhone).Take(2).ToList();
            if (phoneMatches.Count == 1)
            {
                customer = phoneMatches[0];
            }
        }

        if (customer is null && normalizedDocument is null && normalizedPhone.Length > 0)
        {
            customer = db.Customers.SingleOrDefault(item =>
                item.NormalizedName == normalizedName && item.NormalizedPhone == normalizedPhone);
        }

        customer ??= new Customer();
        customer.FirstName = input.FirstName.Trim();
        customer.LastName = input.LastName.Trim();
        customer.Phone = input.Phone.Trim();
        customer.DocumentNumber = string.IsNullOrWhiteSpace(input.DocumentNumber) ? null : input.DocumentNumber.Trim();
        customer.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
        customer.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        customer.NormalizedName = normalizedName;
        customer.NormalizedPhone = normalizedPhone;
        customer.NormalizedDocument = normalizedDocument;
        return customer;
    }

    public IReadOnlyList<SaleRow> GetSales(DateOnly from, DateOnly to)
    {
        using var db = factory.CreateDbContext();
        return LoadSales(db, from, to)
            .OrderByDescending(item => item.CreatedAt)
            .Select(ToSaleRow)
            .ToList();
    }

    public PagedResult<SaleRow> GetSalesPage(DateOnly from, DateOnly to, string? search, int page, int pageSize)
    {
        using var db = factory.CreateDbContext();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 20, 500);
        var fromKey = ToDateKey(from);
        var toKey = ToDateKey(to);
        var query = db.Sales
            .Where(sale => sale.CreatedDateKey >= fromKey && sale.CreatedDateKey <= toKey);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            var parsedId = int.TryParse(search.Trim(), out var saleId) ? saleId : -1;
            query = query.Where(sale => sale.Id == parsedId ||
                (sale.Customer != null &&
                 (EF.Functions.Like(sale.Customer.FirstName, pattern) ||
                  EF.Functions.Like(sale.Customer.LastName, pattern) ||
                  EF.Functions.Like(sale.Customer.Phone, pattern))));
        }

        var totalCount = query.Count();
        var sales = query
            .OrderByDescending(sale => sale.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(item => item.Customer)
            .Include(item => item.Items)
            .Include(item => item.Payments)
            .Include(item => item.Returns)
            .AsNoTracking()
            .ToList()
            .Select(ToSaleRow)
            .ToList();
        return new PagedResult<SaleRow>(sales, page, pageSize, totalCount);
    }

    public IReadOnlyList<PaymentCollectionRow> GetPaymentCollections(DateOnly from, DateOnly to)
    {
        using var db = factory.CreateDbContext();
        var fromKey = ToDateKey(from);
        var toKey = ToDateKey(to);
        var relevantSaleIds = db.Payments.AsNoTracking()
            .Where(payment => payment.PaidDateKey >= fromKey && payment.PaidDateKey <= toKey)
            .Select(payment => payment.SaleId)
            .Distinct()
            .ToList();
        return db.Sales
            .Where(sale => relevantSaleIds.Contains(sale.Id))
            .Include(item => item.Customer)
            .Include(item => item.Items)
            .Include(item => item.Payments)
            .Include(item => item.Returns)
            .AsNoTracking()
            .AsEnumerable()
            .SelectMany(sale => sale.Payments
                .Where(payment => payment.PaidDateKey >= fromKey && payment.PaidDateKey <= toKey)
                .Select(payment => new PaymentCollectionRow(
                    sale.Id,
                    payment.PaidAt,
                    sale.Customer?.FullName ?? "",
                    payment.Method,
                    payment.Amount,
                    payment.Installments,
                    sale.BalanceDue)))
            .OrderByDescending(item => item.PaidAt)
            .ThenByDescending(item => item.SaleId)
            .ToList();
    }

    public string ExportPaymentCollectionsReport(string directory, DateOnly from, DateOnly to)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"cobranzas-{from:yyyyMMdd}-{to:yyyyMMdd}-{DateTime.Now:HHmmss}.xlsx");
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Cobranzas");
        WriteHeaders(worksheet, ["Fecha", "Venta", "Cliente", "Medio", "Monto", "Cuotas", "Saldo venta"]);

        var row = 2;
        foreach (var item in GetPaymentCollections(from, to))
        {
            worksheet.Cell(row, 1).Value = item.PaidAt.DateTime;
            worksheet.Cell(row, 2).Value = item.SaleId;
            worksheet.Cell(row, 3).Value = item.Customer;
            worksheet.Cell(row, 4).Value = FormatPaymentMethod(item.Method);
            worksheet.Cell(row, 5).Value = item.Amount;
            worksheet.Cell(row, 6).Value = item.Installments;
            worksheet.Cell(row, 7).Value = item.SaleBalanceDue;
            row++;
        }

        worksheet.Cell(row + 1, 4).Value = "Total";
        worksheet.Cell(row + 1, 4).Style.Font.Bold = true;
        worksheet.Cell(row + 1, 5).FormulaA1 = $"SUM(E2:E{Math.Max(2, row - 1)})";
        worksheet.Cell(row + 1, 5).Style.Font.Bold = true;
        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
        return path;
    }

    public SaleDetail GetSaleDetail(int saleId)
    {
        using var db = factory.CreateDbContext();
        var sale = db.Sales
            .Include(item => item.Customer)
            .Include(item => item.Items)
            .ThenInclude(item => item.Variant)
            .ThenInclude(item => item.Product)
            .Include(item => item.Payments)
            .Include(item => item.Returns)
            .AsNoTracking()
            .Single(item => item.Id == saleId);

        return new SaleDetail(
            sale.Id,
            sale.CreatedAt,
            sale.Customer?.FullName ?? "",
            sale.Customer?.Phone ?? "",
            sale.DueDate,
            sale.Status,
            sale.GrossTotal,
            sale.ReturnedTotal,
            sale.NetTotal,
            sale.PaidTotal,
            sale.BalanceDue,
            sale.CashToReturn,
            sale.Items.Select(item => new SaleDetailItemRow(
                    item.Variant.Product.DisplayName,
                    item.Variant.Color,
                    item.Variant.Size,
                    item.Quantity,
                    item.ReturnedQuantity,
                    item.UnitPrice,
                    item.ManualDiscount,
                    item.FinalUnitPrice,
                    item.FinalUnitPrice * item.Quantity))
                .ToList(),
            sale.Payments.OrderBy(item => item.PaidAt)
                .Select(item => new SaleDetailPaymentRow(item.PaidAt, item.Method, item.Amount, item.Installments))
                .ToList(),
            sale.Returns.OrderBy(item => item.CreatedAt)
                .Select(item => new SaleDetailReturnRow(item.CreatedAt, item.Quantity, item.Amount, item.CashToReturn, item.Reason))
                .ToList());
    }

    public IReadOnlyList<CustomerDuplicateRow> GetPotentialCustomerDuplicates()
    {
        using var db = factory.CreateDbContext();
        var customers = db.Customers.AsNoTracking()
            .Select(customer => new
            {
                Customer = customer,
                SalesCount = db.Sales.Count(sale => sale.CustomerId == customer.Id)
            })
            .ToList();
        var duplicateIds = customers
            .GroupBy(item => item.Customer.NormalizedDocument is not null
                ? $"D:{item.Customer.NormalizedDocument}"
                : $"N:{item.Customer.NormalizedName}|P:{item.Customer.NormalizedPhone}")
            .Where(group => group.Count() > 1 && !group.Key.EndsWith("|P:", StringComparison.Ordinal))
            .SelectMany(group => group.Select(item => item.Customer.Id))
            .ToHashSet();

        return customers
            .Where(item => duplicateIds.Contains(item.Customer.Id))
            .OrderBy(item => item.Customer.NormalizedName)
            .ThenBy(item => item.Customer.Id)
            .Select(item => new CustomerDuplicateRow(
                item.Customer.Id,
                item.Customer.FullName,
                item.Customer.Phone,
                item.Customer.DocumentNumber,
                item.SalesCount))
            .ToList();
    }

    public void MergeCustomers(int targetCustomerId, IReadOnlyCollection<int> sourceCustomerIds, UserRole? role)
    {
        var sourceIds = sourceCustomerIds.Where(id => id != targetCustomerId).Distinct().ToArray();
        if (sourceIds.Length == 0)
        {
            throw new InvalidOperationException("Selecciona al menos un cliente duplicado para fusionar.");
        }

        using var db = factory.CreateDbContext();
        using var transaction = db.Database.BeginTransaction();
        var target = db.Customers.Single(customer => customer.Id == targetCustomerId);
        var sources = db.Customers.Where(customer => sourceIds.Contains(customer.Id)).ToList();
        if (sources.Count != sourceIds.Length)
        {
            throw new InvalidOperationException("Uno de los clientes seleccionados ya no existe.");
        }

        foreach (var sale in db.Sales.Where(sale => sale.CustomerId != null && sourceIds.Contains(sale.CustomerId.Value)))
        {
            sale.CustomerId = target.Id;
        }

        db.Customers.RemoveRange(sources);
        AuditWriter.Add(db, clock, role, "Fusionar clientes", "Customer", target.Id.ToString(), $"Fusionados clientes {string.Join(", ", sourceIds)} en {target.FullName}.");
        db.SaveChanges();
        transaction.Commit();
    }

    public IReadOnlyList<CustomerDebtRow> GetCustomerDebts()
    {
        using var db = factory.CreateDbContext();
        return db.Sales
            .Include(item => item.Customer)
            .Include(item => item.Items)
            .Include(item => item.Payments)
            .Include(item => item.Returns)
            .AsNoTracking()
            .Where(item => item.Status == SaleStatus.Open || item.Status == SaleStatus.PartiallyPaid)
            .AsEnumerable()
            .Where(item => item.BalanceDue > 0m)
            .GroupBy(item => new
            {
                CustomerId = item.CustomerId,
                Name = string.IsNullOrWhiteSpace(item.Customer?.FullName) ? "Sin cliente" : item.Customer.FullName,
                Phone = item.Customer?.Phone ?? ""
            })
            .Select(group =>
            {
                var nextDueDate = group
                    .Select(item => item.DueDate)
                    .Where(item => item is not null)
                    .OrderBy(item => item)
                    .FirstOrDefault();
                return new CustomerDebtRow(
                    group.Key.Name,
                    group.Key.Phone,
                    group.Count(),
                    group.Sum(item => item.BalanceDue),
                    nextDueDate,
                    BuildDebtStatus(nextDueDate));
            })
            .OrderByDescending(item => item.TotalPending)
            .ThenBy(item => item.Customer)
            .ToList();
    }

    public string ExportSaleDetailReport(int saleId, string directory)
    {
        Directory.CreateDirectory(directory);
        var detail = GetSaleDetail(saleId);
        var path = Path.Combine(directory, $"comprobante-venta-{saleId:000000}-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
        using var workbook = new XLWorkbook();

        var summary = workbook.AddWorksheet("Resumen");
        summary.Cell(1, 1).Value = $"Venta #{detail.SaleId}";
        summary.Cell(1, 1).Style.Font.Bold = true;
        summary.Cell(1, 1).Style.Font.FontSize = 16;
        summary.Cell(2, 1).Value = "Fecha";
        summary.Cell(2, 2).Value = detail.CreatedAt.DateTime;
        summary.Cell(3, 1).Value = "Cliente";
        summary.Cell(3, 2).Value = detail.Customer;
        summary.Cell(4, 1).Value = "Telefono";
        summary.Cell(4, 2).Value = detail.Phone;
        summary.Cell(5, 1).Value = "Vencimiento";
        summary.Cell(5, 2).Value = detail.DueDate?.ToString("dd/MM/yyyy") ?? "";
        summary.Cell(6, 1).Value = "Estado";
        summary.Cell(6, 2).Value = detail.Status.ToString();
        summary.Cell(8, 1).Value = "Bruto";
        summary.Cell(8, 2).Value = detail.GrossTotal;
        summary.Cell(9, 1).Value = "Devuelto";
        summary.Cell(9, 2).Value = detail.ReturnedTotal;
        summary.Cell(10, 1).Value = "Neto";
        summary.Cell(10, 2).Value = detail.NetTotal;
        summary.Cell(11, 1).Value = "Cobrado";
        summary.Cell(11, 2).Value = detail.PaidTotal;
        summary.Cell(12, 1).Value = "Saldo";
        summary.Cell(12, 2).Value = detail.BalanceDue;
        summary.Cell(13, 1).Value = "Dinero a devolver";
        summary.Cell(13, 2).Value = detail.CashToReturn;
        summary.Column(1).Style.Font.Bold = true;
        summary.Columns().AdjustToContents();

        var itemsSheet = workbook.AddWorksheet("Productos");
        WriteHeaders(itemsSheet, ["Producto", "Color", "Talle", "Cantidad", "Devuelto", "Precio unitario", "Descuento", "Precio final", "Total"]);
        var row = 2;
        foreach (var item in detail.Items)
        {
            itemsSheet.Cell(row, 1).Value = item.Product;
            itemsSheet.Cell(row, 2).Value = item.Color;
            itemsSheet.Cell(row, 3).Value = item.Size;
            itemsSheet.Cell(row, 4).Value = item.Quantity;
            itemsSheet.Cell(row, 5).Value = item.ReturnedQuantity;
            itemsSheet.Cell(row, 6).Value = item.UnitPrice;
            itemsSheet.Cell(row, 7).Value = item.ManualDiscount;
            itemsSheet.Cell(row, 8).Value = item.FinalUnitPrice;
            itemsSheet.Cell(row, 9).Value = item.LineTotal;
            row++;
        }
        itemsSheet.Columns().AdjustToContents();

        var paymentsSheet = workbook.AddWorksheet("Pagos");
        WriteHeaders(paymentsSheet, ["Fecha", "Medio", "Monto", "Cuotas"]);
        row = 2;
        foreach (var payment in detail.Payments)
        {
            paymentsSheet.Cell(row, 1).Value = payment.PaidAt.DateTime;
            paymentsSheet.Cell(row, 2).Value = FormatPaymentMethod(payment.Method);
            paymentsSheet.Cell(row, 3).Value = payment.Amount;
            paymentsSheet.Cell(row, 4).Value = payment.Installments;
            row++;
        }
        paymentsSheet.Columns().AdjustToContents();

        var returnsSheet = workbook.AddWorksheet("Devoluciones");
        WriteHeaders(returnsSheet, ["Fecha", "Cantidad", "Monto", "Dinero a devolver", "Motivo"]);
        row = 2;
        foreach (var returnRecord in detail.Returns)
        {
            returnsSheet.Cell(row, 1).Value = returnRecord.CreatedAt.DateTime;
            returnsSheet.Cell(row, 2).Value = returnRecord.Quantity;
            returnsSheet.Cell(row, 3).Value = returnRecord.Amount;
            returnsSheet.Cell(row, 4).Value = returnRecord.CashToReturn;
            returnsSheet.Cell(row, 5).Value = returnRecord.Reason;
            row++;
        }
        returnsSheet.Columns().AdjustToContents();

        workbook.SaveAs(path);
        return path;
    }

    public string BuildSaleReceiptText(int saleId)
    {
        var detail = GetSaleDetail(saleId);
        var business = GetBusinessSettings();
        var builder = new StringBuilder();
        builder.AppendLine(string.IsNullOrWhiteSpace(business.Name) ? "FPCalzados" : business.Name);
        if (!string.IsNullOrWhiteSpace(business.Address))
        {
            builder.AppendLine(business.Address);
        }

        if (!string.IsNullOrWhiteSpace(business.Phone))
        {
            builder.AppendLine($"Tel: {business.Phone}");
        }

        if (!string.IsNullOrWhiteSpace(business.TaxId))
        {
            builder.AppendLine($"CUIT/DNI: {business.TaxId}");
        }

        builder.AppendLine("TICKET DE VENTA");
        builder.AppendLine(new string('-', 32));
        builder.AppendLine($"Venta: #{detail.SaleId:000000}");
        builder.AppendLine($"Fecha: {detail.CreatedAt:dd/MM/yyyy HH:mm}");
        if (!string.IsNullOrWhiteSpace(detail.Customer))
        {
            builder.AppendLine($"Cliente: {detail.Customer}");
        }

        if (!string.IsNullOrWhiteSpace(detail.Phone))
        {
            builder.AppendLine($"Tel: {detail.Phone}");
        }

        builder.AppendLine(new string('-', 32));
        foreach (var item in detail.Items)
        {
            var option = string.Join(" ", new[] { item.Color, item.Size }.Where(value => !string.IsNullOrWhiteSpace(value)));
            builder.AppendLine(item.Product);
            if (!string.IsNullOrWhiteSpace(option))
            {
                builder.AppendLine($"  {option}");
            }

            builder.AppendLine($"  {item.Quantity} x {item.FinalUnitPrice:C0} = {item.LineTotal:C0}");
            if (item.ReturnedQuantity > 0)
            {
                builder.AppendLine($"  Devuelto: {item.ReturnedQuantity}");
            }
        }

        builder.AppendLine(new string('-', 32));
        builder.AppendLine($"Bruto:     {detail.GrossTotal:C0}");
        if (detail.ReturnedTotal > 0m)
        {
            builder.AppendLine($"Devuelto:  {detail.ReturnedTotal:C0}");
        }

        builder.AppendLine($"TOTAL:     {detail.NetTotal:C0}");
        builder.AppendLine($"Cobrado:   {detail.PaidTotal:C0}");
        if (detail.BalanceDue > 0m)
        {
            builder.AppendLine($"Saldo:     {detail.BalanceDue:C0}");
            builder.AppendLine($"Vence:     {detail.DueDate?.ToString("dd/MM/yyyy") ?? "Sin fecha"}");
        }

        if (detail.CashToReturn > 0m)
        {
            builder.AppendLine($"A devolver:{detail.CashToReturn:C0}");
        }

        if (detail.Payments.Count > 0)
        {
            builder.AppendLine(new string('-', 32));
            builder.AppendLine("Pagos:");
            foreach (var payment in detail.Payments)
            {
                var installments = payment.Installments > 1 ? $" {payment.Installments} cuotas" : "";
                builder.AppendLine($"  {FormatPaymentMethod(payment.Method)}{installments}: {payment.Amount:C0}");
            }
        }

        builder.AppendLine(new string('-', 32));
        builder.AppendLine(string.IsNullOrWhiteSpace(business.TicketFooter) ? "Gracias por su compra" : business.TicketFooter);
        return builder.ToString();
    }

    private BusinessSettings GetBusinessSettings()
    {
        using var db = factory.CreateDbContext();
        return db.BusinessSettings.AsNoTracking().FirstOrDefault() ?? new BusinessSettings();
    }

    public string ExportSaleReceiptText(int saleId, string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"ticket-venta-{saleId:000000}-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        File.WriteAllText(path, BuildSaleReceiptText(saleId));
        return path;
    }

    public CustomerHistory GetCustomerHistory(string customer, string phone)
    {
        using var db = factory.CreateDbContext();
        var normalizedCustomer = DataNormalizer.Text(customer);
        var normalizedPhone = DataNormalizer.Phone(phone);
        var matchingCustomers = db.Customers.AsNoTracking()
            .Where(item => normalizedPhone.Length > 0
                ? item.NormalizedPhone == normalizedPhone
                : item.NormalizedName == normalizedCustomer)
            .Select(item => item.Id)
            .ToList();
        if (matchingCustomers.Count != 1)
        {
            throw new InvalidOperationException(matchingCustomers.Count == 0
                ? "No se encontraron ventas para ese cliente."
                : "Hay más de un cliente coincidente. Selecciona uno mediante teléfono o documento.");
        }

        var customerId = matchingCustomers[0];
        var sales = db.Sales
            .Include(item => item.Customer)
            .Include(item => item.Items)
            .Include(item => item.Payments)
            .Include(item => item.Returns)
            .AsNoTracking()
            .Where(sale => sale.CustomerId == customerId)
            .AsEnumerable()
            .OrderByDescending(item => item.CreatedAt)
            .ToList();

        if (sales.Count == 0)
        {
            throw new InvalidOperationException("No se encontraron ventas para ese cliente.");
        }

        return new CustomerHistory(
            sales[0].Customer?.FullName ?? customer,
            sales[0].Customer?.Phone ?? phone,
            sales.Sum(item => item.NetTotal),
            sales.Sum(item => item.PaidTotal),
            sales.Sum(item => item.ReturnedTotal),
            sales.Sum(item => item.BalanceDue),
            sales.Select(item => new CustomerHistorySaleRow(
                    item.Id,
                    item.CreatedAt,
                    item.DueDate,
                    item.NetTotal,
                    item.PaidTotal,
                    item.BalanceDue,
                    item.Status))
                .ToList(),
            sales.SelectMany(sale => sale.Payments.Select(payment => new CustomerHistoryPaymentRow(
                    sale.Id,
                    payment.PaidAt,
                    payment.Method,
                    payment.Amount,
                    payment.Installments)))
                .OrderByDescending(item => item.PaidAt)
                .ToList(),
            sales.SelectMany(sale => sale.Returns.Select(returnRecord => new CustomerHistoryReturnRow(
                    sale.Id,
                    returnRecord.CreatedAt,
                    returnRecord.Quantity,
                    returnRecord.Amount,
                    returnRecord.CashToReturn,
                    returnRecord.Reason)))
                .OrderByDescending(item => item.CreatedAt)
                .ToList());
    }

    public string ExportCustomerDebtReport(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"deudas-clientes-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Deudas");
        var headers = new[] { "Cliente", "Telefono", "Ventas pendientes", "Total pendiente", "Proximo vencimiento", "Estado" };
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
            worksheet.Cell(1, index + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var item in GetCustomerDebts())
        {
            worksheet.Cell(row, 1).Value = item.Customer;
            worksheet.Cell(row, 2).Value = item.Phone;
            worksheet.Cell(row, 3).Value = item.PendingSalesCount;
            worksheet.Cell(row, 4).Value = item.TotalPending;
            worksheet.Cell(row, 5).Value = item.NextDueDate?.ToString("dd/MM/yyyy") ?? "";
            worksheet.Cell(row, 6).Value = item.Status;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
        return path;
    }

    public Payment RegisterPayment(int saleId, PaymentRequest request, UserRole? role)
    {
        using var db = factory.CreateDbContext();
        var sale = db.Sales
            .Include(item => item.Items)
            .Include(item => item.Payments)
            .Include(item => item.Returns)
            .Single(item => item.Id == saleId);

        var payment = new SalesService(pricingService, clock).RegisterPayment(sale, request);
        sale.Version++;
        AuditWriter.Add(db, clock, role, "Registrar pago", "Sale", saleId.ToString(), $"Pago registrado. Monto: {payment.Amount}. Medio: {payment.Method}.");
        SaveWithConcurrencyMessage(db, "La venta fue modificada por otra operación. Actualiza los datos antes de registrar el pago.");
        return payment;
    }

    public void UpdateSaleDueDate(int saleId, DateOnly? dueDate, UserRole? role)
    {
        using var db = factory.CreateDbContext();
        var sale = db.Sales.Single(item => item.Id == saleId);
        sale.DueDate = dueDate;
        sale.Version++;
        AuditWriter.Add(db, clock, role, "Cambiar vencimiento", "Sale", saleId.ToString(), $"Nuevo vencimiento: {dueDate?.ToString("dd/MM/yyyy") ?? "Sin vencimiento"}.");
        SaveWithConcurrencyMessage(db, "La venta fue modificada por otra operación.");
    }

    public ReturnRecord ReturnSaleItem(int saleId, int saleItemId, int quantity, string reason, UserRole? role)
    {
        using var db = factory.CreateDbContext();
        var sale = db.Sales
            .Include(item => item.Items)
            .ThenInclude(item => item.Variant)
            .ThenInclude(item => item.Movements)
            .Include(item => item.Payments)
            .Include(item => item.Returns)
            .Single(item => item.Id == saleId);

        var record = new SalesService(pricingService, clock).ReturnItem(sale, saleItemId, quantity, reason);
        sale.Version++;
        record.SaleId = sale.Id;
        var variant = sale.Items.Single(item => item.Id == saleItemId).Variant;
        variant.Version++;
        AuditWriter.Add(db, clock, role, "Registrar devolución", "Sale", saleId.ToString(), $"Devuelto item {saleItemId}, cantidad {quantity}. Motivo: {reason}.");
        SaveWithConcurrencyMessage(db, "La venta o el stock fueron modificados por otra operación. Actualiza los datos antes de devolver.");
        return record;
    }

    public IReadOnlyList<ReturnableSaleItemRow> GetReturnableSaleItems(int saleId)
    {
        using var db = factory.CreateDbContext();
        var sale = db.Sales
            .Include(item => item.Items)
            .ThenInclude(item => item.Variant)
            .ThenInclude(item => item.Product)
            .AsNoTracking()
            .Single(item => item.Id == saleId);

        return sale.Items
            .Select(item => new ReturnableSaleItemRow(
                item.Id,
                item.Variant.Product.DisplayName,
                item.Variant.Color,
                item.Variant.Size,
                item.Quantity,
                item.ReturnedQuantity,
                item.FinalUnitPrice))
            .ToList();
    }

    public static List<Sale> LoadSales(AppDbContext db, DateOnly from, DateOnly to)
    {
        var fromKey = ToDateKey(from);
        var toKey = ToDateKey(to);
        return db.Sales
            .Where(item => item.CreatedDateKey >= fromKey && item.CreatedDateKey <= toKey)
            .Include(item => item.Customer)
            .Include(item => item.Items)
            .ThenInclude(item => item.Variant)
            .ThenInclude(item => item.Product)
            .Include(item => item.Payments)
            .Include(item => item.Returns)
            .AsNoTracking()
            .ToList();
    }

    private static int ToDateKey(DateOnly value) => value.Year * 10_000 + value.Month * 100 + value.Day;

    private static void SaveWithConcurrencyMessage(AppDbContext db, string message)
    {
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InvalidOperationException(message, exception);
        }
    }

    private static void WriteHeaders(IXLWorksheet worksheet, IReadOnlyList<string> headers)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
            worksheet.Cell(1, index + 1).Style.Font.Bold = true;
        }
    }

    private static string FormatPaymentMethod(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Cash => "Efectivo",
            PaymentMethod.Debit => "Debito",
            PaymentMethod.Credit => "Credito",
            _ => method.ToString()
        };
    }

    private string BuildDebtStatus(DateOnly? dueDate)
    {
        if (dueDate is null)
        {
            return "Pendiente";
        }

        return dueDate.Value < DateOnly.FromDateTime(clock.Now.Date)
            ? "Vencido"
            : "Pendiente";
    }

    private static SaleRow ToSaleRow(Sale sale)
    {
        return new SaleRow(
            sale.Id,
            sale.CreatedAt,
            sale.Customer?.FullName ?? "",
            sale.DueDate,
            sale.GrossTotal,
            sale.NetTotal,
            sale.PaidTotal,
            sale.BalanceDue,
            sale.Status);
    }
}
