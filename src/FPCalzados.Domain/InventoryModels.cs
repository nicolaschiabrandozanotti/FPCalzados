namespace FPCalzados.Domain;

public sealed class Product
{
    public int Id { get; set; }
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public decimal SupplierCost { get; set; }
    public decimal CashPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ProductVariant> Variants { get; set; } = [];
    public List<PriceOverride> PriceOverrides { get; set; } = [];

    public string DisplayName => $"{Brand} {Model}".Trim();
}

public sealed class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Brand { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = new();
    public string Color { get; set; } = "";
    public string Size { get; set; } = "";
    public string? Sku { get; set; }
    public decimal? SupplierCostOverride { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; }
    public List<StockMovement> Movements { get; set; } = [];

    public string DisplayName
    {
        get
        {
            var variant = string.Join(" / ", new[] { Color, Size }.Where(item => !string.IsNullOrWhiteSpace(item)));
            return string.IsNullOrWhiteSpace(variant) ? Product.DisplayName : $"{Product.DisplayName} - {variant}";
        }
    }
}

public sealed class PriceOverride
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = new();
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }
}

public sealed class StockMovement
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public ProductVariant Variant { get; set; } = new();
    public StockMovementType Type { get; set; }
    public int QuantityChange { get; set; }
    public int StockAfter { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.Now;
    public string Reason { get; set; } = "";
}
