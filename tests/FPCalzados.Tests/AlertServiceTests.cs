using FPCalzados.Domain;

namespace FPCalzados.Tests;

public sealed class AlertServiceTests
{
    [Fact]
    public void BuildAlerts_flags_low_stock_and_overdue_customer_balances()
    {
        var product = TestData.Product("Kioshi", "Roma", 40000m);
        var variant = TestData.Variant(product, "Negro", "39", stock: 1, lowStockThreshold: 2);
        var sale = new Sale
        {
            Customer = new Customer { FirstName = "Ana", LastName = "Lopez", Phone = "11223344" },
            DueDate = new DateOnly(2026, 5, 10),
            Status = SaleStatus.PartiallyPaid,
            Items = [new SaleItem { Quantity = 1, UnitPrice = 40000m, FinalUnitPrice = 40000m }],
            Payments = [new Payment { Amount = 10000m }]
        };
        var service = new AlertService();

        var alerts = service.BuildAlerts([variant], [sale], new DateOnly(2026, 5, 17));

        Assert.Contains(alerts, alert => alert.Type == AlertType.LowStock && alert.Message.Contains("Kioshi Roma"));
        Assert.Contains(alerts, alert => alert.Type == AlertType.OverduePayment && alert.Message.Contains("Ana Lopez"));
    }
}
