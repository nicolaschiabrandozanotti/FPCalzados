namespace FPCalzados.Domain;

public sealed class PricingService
{
    public decimal ResolveCashPrice(ProductVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        var product = variant.Product ?? throw new InvalidOperationException("La variante no tiene producto asociado.");

        var exact = product.PriceOverrides.FirstOrDefault(item =>
            Same(item.Color, variant.Color) && Same(item.Size, variant.Size));
        if (exact is not null)
        {
            return exact.Price;
        }

        var colorOnly = product.PriceOverrides.FirstOrDefault(item =>
            Same(item.Color, variant.Color) && string.IsNullOrWhiteSpace(item.Size));
        if (colorOnly is not null)
        {
            return colorOnly.Price;
        }

        var sizeOnly = product.PriceOverrides.FirstOrDefault(item =>
            string.IsNullOrWhiteSpace(item.Color) && Same(item.Size, variant.Size));
        return sizeOnly?.Price ?? product.CashPrice;
    }

    public PaymentCalculation CalculatePayment(decimal cashPrice, PaymentRule rule, decimal manualDiscount)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (cashPrice < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(cashPrice), "El precio no puede ser negativo.");
        }

        if (manualDiscount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(manualDiscount), "El descuento no puede ser negativo.");
        }

        var baseAfterDiscount = Math.Max(0m, cashPrice - manualDiscount);
        var total = decimal.Round(baseAfterDiscount * (1m + rule.PercentModifier / 100m), 2, MidpointRounding.AwayFromZero);
        var installments = Math.Max(1, rule.Installments);
        var installmentAmount = decimal.Round(total / installments, 2, MidpointRounding.AwayFromZero);
        return new PaymentCalculation(baseAfterDiscount, total, installmentAmount);
    }

    private static bool Same(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
