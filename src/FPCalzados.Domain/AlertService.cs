namespace FPCalzados.Domain;

public sealed record AppAlert(AlertType Type, string Message, string Severity);

public sealed class AlertService
{
    public IReadOnlyList<AppAlert> BuildAlerts(
        IEnumerable<ProductVariant> variants,
        IEnumerable<Sale> sales,
        DateOnly today)
    {
        var alerts = new List<AppAlert>();

        alerts.AddRange(variants
            .Where(variant => variant.IsActive && variant.StockQuantity <= variant.LowStockThreshold)
            .Select(variant => new AppAlert(
                AlertType.LowStock,
                $"{variant.Product.DisplayName} {variant.Color} {variant.Size}: stock {variant.StockQuantity}",
                "warning")));

        alerts.AddRange(sales
            .Where(sale => sale.BalanceDue > 0m && sale.DueDate is not null && sale.DueDate.Value < today)
            .Select(sale => new AppAlert(
                AlertType.OverduePayment,
                $"{sale.Customer?.FullName ?? "Cliente sin nombre"} debe {sale.BalanceDue:C0} desde {sale.DueDate:dd/MM/yyyy}",
                "danger")));

        return alerts;
    }
}
