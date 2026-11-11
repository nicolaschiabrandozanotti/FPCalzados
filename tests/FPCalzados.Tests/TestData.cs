using FPCalzados.Domain;

namespace FPCalzados.Tests;

internal static class TestData
{
    public static Product Product(string brand, string model, decimal cashPrice)
    {
        return new Product
        {
            Brand = brand,
            Model = model,
            SupplierCost = cashPrice / 2m,
            CashPrice = cashPrice
        };
    }

    public static ProductVariant Variant(Product product, string color, string size, int stock, int lowStockThreshold = 1)
    {
        var variant = new ProductVariant
        {
            Product = product,
            Color = color,
            Size = size,
            StockQuantity = stock,
            LowStockThreshold = lowStockThreshold
        };
        product.Variants.Add(variant);
        return variant;
    }
}
