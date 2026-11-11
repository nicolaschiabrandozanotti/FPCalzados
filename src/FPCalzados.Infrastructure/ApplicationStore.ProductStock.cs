using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

public sealed partial class ApplicationStore
{
    public void SaveProductVariant(ProductEditRequest request)
    {
        RequireAdmin();
        inventory.SaveProductVariant(request, currentRole);
    }

    public void SaveProductStock(ProductStockEditRequest request)
    {
        RequireAdmin();
        inventory.SaveProductStock(request, currentRole);
    }
}
