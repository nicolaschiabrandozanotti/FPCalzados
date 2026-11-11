namespace FPCalzados.Domain;

public sealed class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? DocumentNumber { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string NormalizedName { get; set; } = "";
    public string NormalizedPhone { get; set; } = "";
    public string? NormalizedDocument { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public sealed class Sale
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public int CreatedDateKey { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateOnly? DueDate { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Open;
    public int Version { get; set; }
    public List<SaleItem> Items { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<ReturnRecord> Returns { get; set; } = [];

    public decimal GrossTotal => Items.Sum(item => item.FinalUnitPrice * item.Quantity);
    public decimal ReturnedTotal => Returns.Sum(item => item.Amount);
    public decimal NetTotal => Math.Max(0m, GrossTotal - ReturnedTotal);
    public decimal PaidTotal => Payments.Sum(payment => payment.Amount);
    public decimal BalanceDue => Math.Max(0m, NetTotal - PaidTotal);
    public decimal CashToReturn => Math.Max(0m, PaidTotal - NetTotal);
}

public sealed class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductVariantId { get; set; }
    public ProductVariant Variant { get; set; } = new();
    public int Quantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ManualDiscount { get; set; }
    public decimal FinalUnitPrice { get; set; }
    public decimal SupplierCostSnapshot { get; set; }
    public PaymentMethod PaymentMethodSnapshot { get; set; }
    public int InstallmentsSnapshot { get; set; } = 1;
    public decimal PercentModifierSnapshot { get; set; }
}

public sealed class Payment
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public int Installments { get; set; } = 1;
    public DateTimeOffset PaidAt { get; set; } = DateTimeOffset.Now;
    public int PaidDateKey { get; set; }
}

public sealed class ReturnRecord
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int SaleItemId { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal CashToReturn { get; set; }
    public string Reason { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}

public sealed class CashRegisterSession
{
    public int Id { get; set; }
    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? ClosingCash { get; set; }
    public string? Notes { get; set; }
    public int Version { get; set; }

    public bool IsOpen => ClosedAt is null;
}

public sealed class CreateSaleRequest
{
    public IReadOnlyList<SaleItemRequest> Items { get; init; } = [];
    public IReadOnlyList<PaymentRequest> Payments { get; init; } = [];
    public Customer? Customer { get; init; }
    public DateOnly? DueDate { get; init; }
}

public sealed class SaleItemRequest
{
    public ProductVariant Variant { get; init; } = new();
    public int Quantity { get; init; }
    public decimal ManualDiscount { get; init; }
    public PaymentRule PaymentRule { get; init; } = PaymentRule.Cash;
}

public sealed record PaymentRequest(PaymentMethod Method, decimal Amount, int Installments);
