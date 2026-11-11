using FPCalzados.Domain;
using Microsoft.EntityFrameworkCore;

namespace FPCalzados.Infrastructure;

internal sealed class InventoryApplicationService(
    AppDbContextFactory factory,
    PricingService pricingService,
    IClock clock)
{
    public IReadOnlyList<InventoryRow> GetInventory(string? search = null, bool includeInactive = false)
    {
        using var db = factory.CreateDbContext();
        var variants = db.ProductVariants
            .Include(item => item.Product)
            .ThenInclude(item => item.PriceOverrides)
            .AsNoTracking()
            .Where(item => includeInactive || (item.IsActive && item.Product.IsActive));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            var supplierBrands = db.Suppliers.AsNoTracking()
                .Where(item => item.IsActive && EF.Functions.Like(item.Name, pattern))
                .Select(item => item.Brand)
                .ToList();
            variants = variants.Where(item =>
                EF.Functions.Like(item.Product.Brand, pattern) ||
                EF.Functions.Like(item.Product.Model, pattern) ||
                EF.Functions.Like(item.Color, pattern) ||
                EF.Functions.Like(item.Size, pattern) ||
                supplierBrands.Contains(item.Product.Brand) ||
                (item.Sku != null && EF.Functions.Like(item.Sku, pattern)));
        }

        return variants
            .OrderBy(item => item.Product.Brand)
            .ThenBy(item => item.Product.Model)
            .ThenBy(item => item.Color)
            .ThenBy(item => item.Size)
            .ToList()
            .Select(item => new InventoryRow(
                item.ProductId,
                item.Id,
                item.Product.Brand,
                item.Product.Model,
                item.Color,
                item.Size,
                item.Sku,
                item.StockQuantity,
                item.LowStockThreshold,
                item.SupplierCostOverride ?? item.Product.SupplierCost,
                pricingService.ResolveCashPrice(item),
                item.IsActive && item.Product.IsActive))
            .ToList();
    }

    public decimal GetVariantCashPrice(int variantId)
    {
        using var db = factory.CreateDbContext();
        var variant = db.ProductVariants
            .Include(item => item.Product)
            .ThenInclude(item => item.PriceOverrides)
            .AsNoTracking()
            .Single(item => item.Id == variantId);
        return pricingService.ResolveCashPrice(variant);
    }

    public IReadOnlyList<StockMovementRow> GetStockMovements(int? productId = null, int? variantId = null, int take = 200)
    {
        using var db = factory.CreateDbContext();
        var query = db.StockMovements
            .Include(item => item.Variant)
            .ThenInclude(item => item.Product)
            .AsNoTracking();

        if (variantId is not null)
        {
            query = query.Where(item => item.ProductVariantId == variantId.Value);
        }
        else if (productId is not null)
        {
            query = query.Where(item => item.Variant.ProductId == productId.Value);
        }

        return query
            .OrderByDescending(item => item.Id)
            .Take(Math.Clamp(take, 1, 1000))
            .AsEnumerable()
            .Select(item => new StockMovementRow(
                item.Id,
                item.OccurredAt,
                item.Variant.Product.Brand,
                item.Variant.Product.Model,
                item.Variant.Color,
                item.Variant.Size,
                item.Variant.Sku,
                item.Type,
                item.QuantityChange,
                item.StockAfter,
                item.Reason))
            .ToList();
    }

    public void SaveProductVariant(ProductEditRequest request, UserRole? role)
    {
        ValidateProductRequest(request);
        using var db = factory.CreateDbContext();
        var hasVariantDimension = !string.IsNullOrWhiteSpace(request.Color) || !string.IsNullOrWhiteSpace(request.Size);
        var product = request.ProductId is int productId
            ? db.Products.Include(item => item.Variants).Single(item => item.Id == productId)
            : FindProduct(db, request.Brand, request.Model) ?? new Product();

        product.Brand = request.Brand.Trim();
        product.Model = request.Model.Trim();
        if (product.Id == 0 || !hasVariantDimension)
        {
            product.SupplierCost = request.SupplierCost;
        }

        if (product.Id == 0 || !hasVariantDimension)
        {
            product.CashPrice = request.CashPrice;
        }

        product.IsActive = true;

        if (product.Id == 0)
        {
            db.Products.Add(product);
        }

        var variant = request.VariantId is int variantId
            ? db.ProductVariants.Single(item => item.Id == variantId)
            : product.Variants.FirstOrDefault(item =>
                Same(item.Color, request.Color) && Same(item.Size, request.Size)) ?? new ProductVariant();

        var oldStock = variant.Id == 0 ? 0 : variant.StockQuantity;
        variant.Product = product;
        variant.Color = request.Color.Trim();
        variant.Size = request.Size.Trim();
        variant.Sku = NormalizeSku(request.Sku);
        variant.SupplierCostOverride = hasVariantDimension ? request.SupplierCost : null;
        variant.StockQuantity = request.StockQuantity;
        variant.LowStockThreshold = request.LowStockThreshold;
        variant.IsActive = true;

        if (variant.Id == 0)
        {
            product.Variants.Add(variant);
        }

        foreach (var productVariant in product.Variants)
        {
            productVariant.LowStockThreshold = request.LowStockThreshold;
        }

        if (hasVariantDimension)
        {
            SavePriceOverride(db, product, request.Color, request.Size, request.CashPrice);
        }

        var difference = request.StockQuantity - oldStock;
        if (difference != 0)
        {
            if (variant.Id != 0)
            {
                variant.Version++;
            }

            variant.Movements.Add(new StockMovement
            {
                Variant = variant,
                Type = variant.Id == 0 ? StockMovementType.Purchase : StockMovementType.Adjustment,
                QuantityChange = difference,
                StockAfter = request.StockQuantity,
                OccurredAt = clock.Now,
                Reason = variant.Id == 0 ? "Carga inicial" : "Ajuste manual"
            });
        }

        AuditWriter.Add(db, clock, role, "Guardar variante", "Product", request.ProductId?.ToString(), $"Producto '{request.Brand} {request.Model}' variante '{request.Color}/{request.Size}' guardado.");
        SaveWithConcurrencyMessage(db, "El producto o su stock fueron modificados por otra operación.");
    }

    public void SaveProductStock(ProductStockEditRequest request, UserRole? role)
    {
        ValidateProductStockRequest(request);
        using var db = factory.CreateDbContext();
        var product = request.ProductId is int productId
            ? db.Products
                .Include(item => item.Variants)
                .Include(item => item.PriceOverrides)
                .Single(item => item.Id == productId)
            : FindProduct(db, request.Brand, request.Model) ?? new Product();

        if (product.Id != 0)
        {
            db.Entry(product).Collection(item => item.PriceOverrides).Load();
        }

        product.Brand = request.Brand.Trim();
        product.Model = request.Model.Trim();
        product.SupplierCost = request.SupplierCost ?? 0m;
        product.CashPrice = request.CashPrice;
        product.IsActive = true;

        if (product.Id == 0)
        {
            db.Products.Add(product);
        }

        if (product.PriceOverrides.Count > 0)
        {
            db.PriceOverrides.RemoveRange(product.PriceOverrides);
            product.PriceOverrides.Clear();
        }

        var requestedVariants = new List<ProductVariant>();
        foreach (var variantRequest in request.Variants)
        {
            var variant = variantRequest.VariantId is int variantId
                ? product.Variants.Single(item => item.Id == variantId)
                : product.Variants.FirstOrDefault(item =>
                    Same(item.Color, variantRequest.Color) && Same(item.Size, variantRequest.Size)) ?? new ProductVariant();
            requestedVariants.Add(variant);

            var oldStock = variant.Id == 0 ? 0 : variant.StockQuantity;
            variant.Product = product;
            variant.Color = variantRequest.Color.Trim();
            variant.Size = variantRequest.Size.Trim();
            variant.Sku = NormalizeSku(variantRequest.Sku);
            variant.SupplierCostOverride = null;
            variant.StockQuantity = variantRequest.StockQuantity;
            variant.LowStockThreshold = request.LowStockThreshold;
            variant.IsActive = true;

            if (variant.Id == 0)
            {
                product.Variants.Add(variant);
            }

            var difference = variantRequest.StockQuantity - oldStock;
            if (difference != 0)
            {
                if (variant.Id != 0)
                {
                    variant.Version++;
                }

                variant.Movements.Add(new StockMovement
                {
                    Variant = variant,
                    Type = variant.Id == 0 ? StockMovementType.Purchase : StockMovementType.Adjustment,
                    QuantityChange = difference,
                    StockAfter = variantRequest.StockQuantity,
                    OccurredAt = clock.Now,
                    Reason = variant.Id == 0 ? "Carga inicial" : "Ajuste manual"
                });
            }
        }

        foreach (var existingVariant in product.Variants)
        {
            existingVariant.LowStockThreshold = request.LowStockThreshold;
            if (existingVariant.Id != 0 && existingVariant.IsActive && !requestedVariants.Contains(existingVariant))
            {
                existingVariant.IsActive = false;
            }
        }

        AuditWriter.Add(db, clock, role, "Guardar stock", "Product", request.ProductId?.ToString(), $"Producto '{request.Brand} {request.Model}' guardado con {request.Variants.Count} variantes.");
        SaveWithConcurrencyMessage(db, "El producto o su stock fueron modificados por otra operación.");
    }

    public void DeactivateVariant(int variantId, UserRole? role)
    {
        using var db = factory.CreateDbContext();
        var variant = db.ProductVariants.Single(item => item.Id == variantId);
        variant.IsActive = false;
        variant.Version++;
        AuditWriter.Add(db, clock, role, "Dar de baja variante", "ProductVariant", variantId.ToString(), "Variante dada de baja.");
        SaveWithConcurrencyMessage(db, "La variante fue modificada por otra operación.");
    }

    public StockMovement AdjustVariantStock(int variantId, int quantityChange, StockMovementType type, string reason, UserRole? role)
    {
        if (quantityChange == 0)
        {
            throw new InvalidOperationException("La cantidad del movimiento no puede ser cero.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Carga un motivo para el movimiento de stock.");
        }

        using var db = factory.CreateDbContext();
        var variant = db.ProductVariants
            .Include(item => item.Movements)
            .Single(item => item.Id == variantId);
        var stockAfter = variant.StockQuantity + quantityChange;
        if (stockAfter < 0)
        {
            throw new InvalidOperationException("El movimiento deja el stock en negativo.");
        }

        variant.StockQuantity = stockAfter;
        variant.Version++;
        var movement = new StockMovement
        {
            Variant = variant,
            Type = type,
            QuantityChange = quantityChange,
            StockAfter = stockAfter,
            OccurredAt = clock.Now,
            Reason = reason.Trim()
        };
        variant.Movements.Add(movement);
        AuditWriter.Add(db, clock, role, "Ajustar stock", "ProductVariant", variantId.ToString(), $"Movimiento {quantityChange}. Stock final: {stockAfter}. Motivo: {reason.Trim()}.");
        SaveWithConcurrencyMessage(db, "El stock fue modificado por otra operación. Actualiza los datos antes de reintentar.");
        return movement;
    }

    public IReadOnlyList<PriceOverrideRow> GetPriceOverrides(int productId)
    {
        using var db = factory.CreateDbContext();
        return db.PriceOverrides.AsNoTracking()
            .Where(item => item.ProductId == productId)
            .OrderBy(item => item.Color)
            .ThenBy(item => item.Size)
            .Select(item => new PriceOverrideRow(item.Id, item.Color, item.Size, item.Price))
            .ToList();
    }

    public void SavePriceOverride(int productId, string? color, string? size, decimal price, UserRole? role)
    {
        if (price < 0m)
        {
            throw new InvalidOperationException("El precio no puede ser negativo.");
        }

        using var db = factory.CreateDbContext();
        var product = db.Products.Single(item => item.Id == productId);
        var existing = db.PriceOverrides.SingleOrDefault(item =>
            item.ProductId == productId &&
            item.Color == NormalizeNullable(color) &&
            item.Size == NormalizeNullable(size));

        if (existing is null)
        {
            db.PriceOverrides.Add(new PriceOverride
            {
                Product = product,
                Color = NormalizeNullable(color),
                Size = NormalizeNullable(size),
                Price = price
            });
        }
        else
        {
            existing.Price = price;
        }

        AuditWriter.Add(db, clock, role, "Guardar precio especial", "Product", productId.ToString(), $"Precio especial guardado para color '{color}' talle '{size}': {price}.");
        db.SaveChanges();
    }

    public void ImportStockRows(IEnumerable<StockImportRow> rows, UserRole? role)
    {
        var materializedRows = rows.ToList();
        using var db = factory.CreateDbContext();
        ValidateStockImportRows(db, materializedRows);
        var productCache = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in materializedRows)
        {
            var productKey = ProductKey(row.Brand, row.Model);
            if (!productCache.TryGetValue(productKey, out var product))
            {
                product = FindProduct(db, row.Brand, row.Model) ?? new Product
                {
                    Brand = row.Brand.Trim(),
                    Model = row.Model.Trim()
                };
                productCache[productKey] = product;
            }

            var hasVariantDimension = !string.IsNullOrWhiteSpace(row.Color) || !string.IsNullOrWhiteSpace(row.Size);
            if (product.Id == 0 || !hasVariantDimension)
            {
                product.SupplierCost = row.SupplierCost;
                product.CashPrice = row.CashPrice;
            }

            product.IsActive = true;
            if (product.Id == 0)
            {
                db.Products.Add(product);
            }

            var color = row.Color.Trim();
            var size = row.Size.Trim();
            var variant = product.Id == 0
                ? product.Variants.FirstOrDefault(item => Same(item.Color, color) && Same(item.Size, size))
                : db.ProductVariants.Where(item => item.ProductId == product.Id)
                    .AsEnumerable()
                    .SingleOrDefault(item => Same(item.Color, color) && Same(item.Size, size));

            variant ??= new ProductVariant
            {
                Product = product,
                Color = color,
                Size = size
            };

            variant.Sku = string.IsNullOrWhiteSpace(row.Sku) ? variant.Sku : NormalizeSku(row.Sku);
            variant.SupplierCostOverride = row.SupplierCost;
            variant.StockQuantity += row.Quantity;
            variant.Version++;
            variant.IsActive = true;
            if (variant.Id == 0)
            {
                product.Variants.Add(variant);
            }

            variant.Movements.Add(new StockMovement
            {
                Variant = variant,
                Type = StockMovementType.Import,
                QuantityChange = row.Quantity,
                StockAfter = variant.StockQuantity,
                OccurredAt = clock.Now,
                Reason = "Importacion XLSX"
            });
        }

        AuditWriter.Add(db, clock, role, "Importar stock", "StockImport", null, $"Importadas {materializedRows.Count} filas de stock.");
        SaveWithConcurrencyMessage(db, "El stock cambió durante la importación. No se aplicó la operación; actualiza los datos y reintenta.");
    }

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

    private static void ValidateStockImportRows(AppDbContext db, IReadOnlyList<StockImportRow> rows)
    {
        foreach (var row in rows.Where(item => !string.IsNullOrWhiteSpace(item.Sku)))
        {
            var sku = NormalizeSku(row.Sku)!;
            var existing = db.ProductVariants
                .Include(item => item.Product)
                .AsNoTracking()
                .SingleOrDefault(item => item.Sku != null && item.Sku.ToUpper() == sku);
            if (existing is null)
            {
                continue;
            }

            var sameVariant = string.Equals(existing.Product.Brand, row.Brand.Trim(), StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(existing.Product.Model, row.Model.Trim(), StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(existing.Color, row.Color.Trim(), StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(existing.Size, row.Size.Trim(), StringComparison.OrdinalIgnoreCase);
            if (!sameVariant)
            {
                var rowLabel = row.SourceRowNumber > 0 ? $"Fila {row.SourceRowNumber}: " : "";
                throw new InvalidOperationException($"{rowLabel}el codigo '{sku}' ya pertenece a {existing.Product.Brand} {existing.Product.Model} {existing.Color} {existing.Size}.");
            }
        }
    }

    private static void SavePriceOverride(AppDbContext db, Product product, string? color, string? size, decimal price)
    {
        var normalizedColor = NormalizeNullable(color);
        var normalizedSize = NormalizeNullable(size);
        var existing = product.Id == 0
            ? product.PriceOverrides.SingleOrDefault(item => item.Color == normalizedColor && item.Size == normalizedSize)
            : db.PriceOverrides.SingleOrDefault(item =>
                item.ProductId == product.Id &&
                item.Color == normalizedColor &&
                item.Size == normalizedSize);

        if (existing is null)
        {
            product.PriceOverrides.Add(new PriceOverride
            {
                Product = product,
                Color = normalizedColor,
                Size = normalizedSize,
                Price = price
            });
        }
        else
        {
            existing.Price = price;
        }
    }

    private static Product? FindProduct(AppDbContext db, string brand, string model)
    {
        var normalizedBrand = DataNormalizer.Text(brand);
        var normalizedModel = DataNormalizer.Text(model);
        return db.Products.Include(item => item.Variants)
            .AsEnumerable()
            .SingleOrDefault(item =>
                DataNormalizer.Text(item.Brand) == normalizedBrand &&
                DataNormalizer.Text(item.Model) == normalizedModel);
    }

    private static string ProductKey(string brand, string model)
    {
        return $"{DataNormalizer.Text(brand)}|{DataNormalizer.Text(model)}";
    }

    private static void ValidateProductRequest(ProductEditRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Brand) ||
            string.IsNullOrWhiteSpace(request.Model))
        {
            throw new InvalidOperationException("Marca y modelo son obligatorios. Color y talle pueden quedar vacios.");
        }

        if (request.StockQuantity < 0 || request.LowStockThreshold < 0)
        {
            throw new InvalidOperationException("El stock no puede ser negativo.");
        }

        if (request.SupplierCost < 0m || request.CashPrice < 0m)
        {
            throw new InvalidOperationException("Los precios no pueden ser negativos.");
        }
    }

    private static void ValidateProductStockRequest(ProductStockEditRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Brand) ||
            string.IsNullOrWhiteSpace(request.Model))
        {
            throw new InvalidOperationException("Marca y modelo son obligatorios.");
        }

        if (request.Variants.Count == 0)
        {
            throw new InvalidOperationException("Agrega al menos una fila de color, talle y stock.");
        }

        if (request.LowStockThreshold < 0 || request.Variants.Any(item => item.StockQuantity < 0))
        {
            throw new InvalidOperationException("El stock no puede ser negativo.");
        }

        if ((request.SupplierCost ?? 0m) < 0m || request.CashPrice < 0m)
        {
            throw new InvalidOperationException("Los precios no pueden ser negativos.");
        }

        var duplicate = request.Variants
            .GroupBy(item => $"{item.Color.Trim().ToUpperInvariant()}|{item.Size.Trim().ToUpperInvariant()}")
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException("No puede haber dos filas con el mismo color y talle.");
        }
    }

    private static bool Same(string left, string right)
    {
        return DataNormalizer.Text(left) == DataNormalizer.Text(right);
    }

    private static string? NormalizeSku(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
