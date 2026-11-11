using FPCalzados.Domain;

namespace FPCalzados.Tests;

public sealed class PricingServiceTests
{
    [Fact]
    public void ResolvePrice_uses_most_specific_variant_override()
    {
        var service = new PricingService();
        var product = new Product
        {
            Id = 1,
            Brand = "Kioshi",
            Model = "Roma",
            SupplierCost = 20000m,
            CashPrice = 40000m,
            PriceOverrides =
            [
                new PriceOverride { Color = "Negro", Price = 42000m },
                new PriceOverride { Size = "39", Price = 41000m },
                new PriceOverride { Color = "Negro", Size = "39", Price = 43000m }
            ]
        };
        var variant = new ProductVariant { Product = product, Color = "Negro", Size = "39" };

        var price = service.ResolveCashPrice(variant);

        Assert.Equal(43000m, price);
    }

    [Fact]
    public void CalculatePayment_applies_card_surcharge_and_installments()
    {
        var service = new PricingService();
        var rule = new PaymentRule
        {
            Name = "Credito 3 cuotas",
            PaymentMethod = PaymentMethod.Credit,
            PercentModifier = 15m,
            Installments = 3
        };

        var result = service.CalculatePayment(100000m, rule, manualDiscount: 10000m);

        Assert.Equal(103500m, result.Total);
        Assert.Equal(34500m, result.InstallmentAmount);
        Assert.Equal(90000m, result.BaseAfterDiscount);
    }

    [Fact]
    public void CalculatePayment_allows_zero_value_gift_sale()
    {
        var service = new PricingService();
        var result = service.CalculatePayment(50000m, PaymentRule.Cash, manualDiscount: 50000m);

        Assert.Equal(0m, result.Total);
        Assert.Equal(0m, result.InstallmentAmount);
    }
}
