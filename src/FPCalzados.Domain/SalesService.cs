namespace FPCalzados.Domain;

public sealed class SalesService(PricingService pricingService, IClock clock)
{
    private int _nextSaleItemId = 1;
    private int _nextReturnId = 1;

    public Sale CreateSale(CreateSaleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("La venta debe tener al menos un producto.");
        }

        var sale = new Sale
        {
            CreatedAt = clock.Now,
            CreatedDateKey = ToDateKey(clock.Now),
            Customer = request.Customer,
            DueDate = request.DueDate
        };

        foreach (var itemRequest in request.Items)
        {
            AddItem(sale, itemRequest);
        }

        foreach (var paymentRequest in request.Payments)
        {
            if (paymentRequest.Amount <= 0m)
            {
                throw new InvalidOperationException("Cada pago debe ser mayor a cero.");
            }

            sale.Payments.Add(new Payment
            {
                Method = paymentRequest.Method,
                Amount = paymentRequest.Amount,
                Installments = Math.Max(1, paymentRequest.Installments),
                PaidAt = clock.Now,
                PaidDateKey = ToDateKey(clock.Now)
            });
        }

        if (sale.BalanceDue > 0m && sale.Customer is null)
        {
            throw new InvalidOperationException("Una venta con saldo pendiente requiere cliente.");
        }

        sale.Status = sale.BalanceDue == 0m ? SaleStatus.Paid : SaleStatus.PartiallyPaid;
        return sale;
    }

    public Payment RegisterPayment(Sale sale, PaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(sale);
        if (request.Amount <= 0m)
        {
            throw new InvalidOperationException("El pago debe ser mayor a cero.");
        }

        if (request.Amount > sale.BalanceDue)
        {
            throw new InvalidOperationException("El pago no puede superar el saldo pendiente.");
        }

        if (sale.Status is SaleStatus.Returned or SaleStatus.Cancelled)
        {
            throw new InvalidOperationException("No se puede registrar un pago sobre una venta devuelta o cancelada.");
        }

        var payment = new Payment
        {
            Method = request.Method,
            Amount = request.Amount,
            Installments = Math.Max(1, request.Installments),
            PaidAt = clock.Now,
            PaidDateKey = ToDateKey(clock.Now)
        };
        sale.Payments.Add(payment);
        sale.Status = sale.BalanceDue == 0m ? SaleStatus.Paid : SaleStatus.PartiallyPaid;
        return payment;
    }

    public ReturnRecord ReturnItem(Sale sale, int saleItemId, int quantity, string reason)
    {
        ArgumentNullException.ThrowIfNull(sale);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad devuelta debe ser positiva.");
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 240)
        {
            throw new InvalidOperationException("El motivo de devolución es obligatorio y admite hasta 240 caracteres.");
        }

        reason = reason.Trim();
        var item = sale.Items.Single(item => item.Id == saleItemId);
        var availableToReturn = item.Quantity - item.ReturnedQuantity;
        if (quantity > availableToReturn)
        {
            throw new InvalidOperationException("No se puede devolver más cantidad que la vendida.");
        }

        var cashToReturnBefore = sale.CashToReturn;
        item.ReturnedQuantity += quantity;
        item.Variant.StockQuantity += quantity;
        item.Variant.Movements.Add(new StockMovement
        {
            Variant = item.Variant,
            ProductVariantId = item.Variant.Id,
            Type = StockMovementType.Return,
            QuantityChange = quantity,
            StockAfter = item.Variant.StockQuantity,
            OccurredAt = clock.Now,
            Reason = reason
        });

        var amount = item.FinalUnitPrice * quantity;
        var record = new ReturnRecord
        {
            Id = _nextReturnId++,
            SaleItemId = saleItemId,
            Quantity = quantity,
            Amount = amount,
            Reason = reason,
            CreatedAt = clock.Now
        };
        sale.Returns.Add(record);
        record.CashToReturn = Math.Max(0m, sale.CashToReturn - cashToReturnBefore);

        sale.Status = sale.Items.All(saleItem => saleItem.ReturnedQuantity == saleItem.Quantity)
            ? SaleStatus.Returned
            : sale.BalanceDue == 0m ? SaleStatus.Paid : SaleStatus.PartiallyPaid;

        return record;
    }

    private static int ToDateKey(DateTimeOffset value) => value.Year * 10_000 + value.Month * 100 + value.Day;

    private void AddItem(Sale sale, SaleItemRequest itemRequest)
    {
        if (itemRequest.Quantity <= 0)
        {
            throw new InvalidOperationException("La cantidad debe ser positiva.");
        }

        if (itemRequest.Variant.StockQuantity < itemRequest.Quantity)
        {
            throw new InvalidOperationException($"Stock insuficiente para {itemRequest.Variant.DisplayName}.");
        }

        var unitPrice = pricingService.ResolveCashPrice(itemRequest.Variant);
        var supplierCost = itemRequest.Variant.SupplierCostOverride ?? itemRequest.Variant.Product.SupplierCost;
        var calculation = pricingService.CalculatePayment(unitPrice, itemRequest.PaymentRule, itemRequest.ManualDiscount);
        itemRequest.Variant.StockQuantity -= itemRequest.Quantity;
        itemRequest.Variant.Movements.Add(new StockMovement
        {
            Variant = itemRequest.Variant,
            ProductVariantId = itemRequest.Variant.Id,
            Type = StockMovementType.Sale,
            QuantityChange = -itemRequest.Quantity,
            StockAfter = itemRequest.Variant.StockQuantity,
            OccurredAt = clock.Now,
            Reason = "Venta"
        });

        sale.Items.Add(new SaleItem
        {
            Id = _nextSaleItemId++,
            Variant = itemRequest.Variant,
            ProductVariantId = itemRequest.Variant.Id,
            Quantity = itemRequest.Quantity,
            UnitPrice = unitPrice,
            ManualDiscount = itemRequest.ManualDiscount,
            FinalUnitPrice = calculation.Total,
            SupplierCostSnapshot = supplierCost,
            PaymentMethodSnapshot = itemRequest.PaymentRule.PaymentMethod,
            InstallmentsSnapshot = Math.Max(1, itemRequest.PaymentRule.Installments),
            PercentModifierSnapshot = itemRequest.PaymentRule.PercentModifier
        });
    }
}
