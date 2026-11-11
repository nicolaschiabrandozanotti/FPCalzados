using FPCalzados.Domain;

namespace FPCalzados.Tests;

public sealed class SalesWorkflowTests
{
    [Fact]
    public void CreateSale_decrements_stock_and_records_customer_debt()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero));
        var product = TestData.Product("Kioshi", "Roma", 40000m);
        var variant = TestData.Variant(product, "Negro", "39", stock: 2);
        var service = new SalesService(new PricingService(), clock);

        var sale = service.CreateSale(new CreateSaleRequest
        {
            Items =
            [
                new SaleItemRequest
                {
                    Variant = variant,
                    Quantity = 1,
                    ManualDiscount = 0m,
                    PaymentRule = PaymentRule.Cash
                }
            ],
            Payments = [new PaymentRequest(PaymentMethod.Cash, 15000m, 1)],
            Customer = new Customer { FirstName = "Ana", LastName = "Lopez", Phone = "11223344" },
            DueDate = new DateOnly(2026, 5, 25)
        });

        Assert.Equal(1, variant.StockQuantity);
        Assert.Equal(40000m, sale.GrossTotal);
        Assert.Equal(15000m, sale.PaidTotal);
        Assert.Equal(25000m, sale.BalanceDue);
        Assert.Equal(SaleStatus.PartiallyPaid, sale.Status);
        Assert.Equal(20000m, sale.Items[0].SupplierCostSnapshot);
        Assert.Single(variant.Movements);
        Assert.Equal(StockMovementType.Sale, variant.Movements[0].Type);
    }

    [Fact]
    public void ReturnItem_restores_stock_and_reduces_balance_due()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero));
        var product = TestData.Product("Kioshi", "Roma", 40000m);
        var variant = TestData.Variant(product, "Negro", "39", stock: 2);
        var service = new SalesService(new PricingService(), clock);
        var sale = service.CreateSale(new CreateSaleRequest
        {
            Items =
            [
                new SaleItemRequest
                {
                    Variant = variant,
                    Quantity = 1,
                    ManualDiscount = 0m,
                    PaymentRule = PaymentRule.Cash
                }
            ],
            Payments = [new PaymentRequest(PaymentMethod.Cash, 15000m, 1)],
            Customer = new Customer { FirstName = "Ana", LastName = "Lopez", Phone = "11223344" },
            DueDate = new DateOnly(2026, 5, 25)
        });

        var refund = service.ReturnItem(sale, sale.Items[0].Id, quantity: 1, reason: "Arrepentimiento");

        Assert.Equal(2, variant.StockQuantity);
        Assert.Equal(0m, sale.NetTotal);
        Assert.Equal(0m, sale.BalanceDue);
        Assert.Equal(15000m, refund.CashToReturn);
        Assert.Equal(SaleStatus.Returned, sale.Status);
        Assert.Contains(variant.Movements, movement => movement.Type == StockMovementType.Return);
    }
}
