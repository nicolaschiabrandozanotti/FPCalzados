using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

public sealed partial class ApplicationStore
{
    public IReadOnlyList<PriceOverrideRow> GetPriceOverrides(int productId)
    {
        return inventory.GetPriceOverrides(productId);
    }

    public void SavePriceOverride(int productId, string? color, string? size, decimal price)
    {
        RequireAdmin();
        inventory.SavePriceOverride(productId, color, size, price, currentRole);
    }
}
