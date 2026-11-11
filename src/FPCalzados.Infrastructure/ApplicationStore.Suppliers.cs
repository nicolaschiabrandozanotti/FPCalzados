using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

public sealed partial class ApplicationStore
{
    public IReadOnlyList<SupplierRow> GetSuppliers(bool includeInactive = false)
    {
        return suppliers.GetSuppliers(includeInactive);
    }

    public void SaveSupplier(SupplierEditRequest request)
    {
        RequireAdmin();
        suppliers.SaveSupplier(request, currentRole);
    }

    public void DeactivateSupplier(int id)
    {
        RequireAdmin();
        suppliers.DeactivateSupplier(id, currentRole);
    }
}
