using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

public sealed partial class ApplicationStore
{
    public IReadOnlyList<InventoryRow> GetInventory(string? search = null, bool includeInactive = false)
    {
        return inventory.GetInventory(search, includeInactive);
    }

    public decimal GetVariantCashPrice(int variantId)
    {
        return inventory.GetVariantCashPrice(variantId);
    }

    public void DeactivateVariant(int variantId)
    {
        RequireAdmin();
        inventory.DeactivateVariant(variantId, currentRole);
    }

    public StockMovement AdjustVariantStock(int variantId, int quantityChange, StockMovementType type, string reason)
    {
        RequireAdmin();
        return inventory.AdjustVariantStock(variantId, quantityChange, type, reason, currentRole);
    }

    public IReadOnlyList<StockMovementRow> GetStockMovements(int? productId = null, int? variantId = null, int take = 200)
    {
        return inventory.GetStockMovements(productId, variantId, take);
    }
}
