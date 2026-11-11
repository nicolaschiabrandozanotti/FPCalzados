using FPCalzados.Domain;
using Microsoft.EntityFrameworkCore;

namespace FPCalzados.Infrastructure;

internal sealed class SupplierApplicationService(AppDbContextFactory factory, IClock clock)
{
    public IReadOnlyList<SupplierRow> GetSuppliers(bool includeInactive = false)
    {
        using var db = factory.CreateDbContext();
        return db.Suppliers.AsNoTracking()
            .Where(item => includeInactive || item.IsActive)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Brand)
            .Select(item => new SupplierRow(item.Id, item.Name, item.Brand, item.IsActive))
            .ToList();
    }

    public void SaveSupplier(SupplierEditRequest request, UserRole? role)
    {
        ValidateSupplierRequest(request);
        using var db = factory.CreateDbContext();
        var supplier = request.Id is int id
            ? db.Suppliers.Single(item => item.Id == id)
            : db.Suppliers.AsEnumerable().SingleOrDefault(item =>
                DataNormalizer.Text(item.Name) == DataNormalizer.Text(request.Name) &&
                DataNormalizer.Text(item.Brand) == DataNormalizer.Text(request.Brand)) ?? new Supplier();

        supplier.Name = request.Name.Trim();
        supplier.Brand = request.Brand.Trim();
        supplier.IsActive = request.IsActive;

        if (supplier.Id == 0)
        {
            db.Suppliers.Add(supplier);
        }

        AuditWriter.Add(db, clock, role, "Guardar proveedor", "Supplier", supplier.Id == 0 ? null : supplier.Id.ToString(), $"Proveedor '{supplier.Name}' para marca '{supplier.Brand}' guardado.");
        db.SaveChanges();
    }

    public void DeactivateSupplier(int id, UserRole? role)
    {
        using var db = factory.CreateDbContext();
        var supplier = db.Suppliers.Single(item => item.Id == id);
        supplier.IsActive = false;
        AuditWriter.Add(db, clock, role, "Dar de baja proveedor", "Supplier", id.ToString(), "Proveedor dado de baja.");
        db.SaveChanges();
    }

    private static void ValidateSupplierRequest(SupplierEditRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Brand))
        {
            throw new InvalidOperationException("Proveedor y marca son obligatorios.");
        }
    }
}
