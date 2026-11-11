using System.Collections.ObjectModel;
using FPCalzados.Infrastructure;

namespace FPCalzados.App.ViewModels;

public sealed class SuppliersViewModel(ApplicationStore store) : BaseViewModel
{
    public ObservableCollection<SupplierRow> Suppliers { get; } = [];

    public async Task LoadAsync()
    {
        var rows = await Task.Run(() => store.GetSuppliers());
        SetSuppliers(rows);
    }

    public void Load()
    {
        SetSuppliers(store.GetSuppliers());
    }

    public void SaveSupplier(SupplierEditRequest request)
    {
        store.SaveSupplier(request);
    }

    public Task SaveSupplierAsync(SupplierEditRequest request)
    {
        return Task.Run(() => store.SaveSupplier(request));
    }

    public void DeactivateSupplier(int supplierId)
    {
        store.DeactivateSupplier(supplierId);
    }

    public Task DeactivateSupplierAsync(int supplierId)
    {
        return Task.Run(() => store.DeactivateSupplier(supplierId));
    }

    private void SetSuppliers(IEnumerable<SupplierRow> rows)
    {
        Suppliers.Clear();
        foreach (var row in rows)
        {
            Suppliers.Add(row);
        }
    }
}
