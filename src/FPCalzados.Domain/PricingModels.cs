namespace FPCalzados.Domain;

public sealed class PaymentRule
{
    public static PaymentRule Cash { get; } = new()
    {
        Name = "Efectivo",
        PaymentMethod = PaymentMethod.Cash,
        PercentModifier = 0m,
        Installments = 1
    };

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public PaymentMethod PaymentMethod { get; set; }
    public decimal PercentModifier { get; set; }
    public int Installments { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public sealed record PaymentCalculation(decimal BaseAfterDiscount, decimal Total, decimal InstallmentAmount);

public sealed class Promotion
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public decimal PercentDiscount { get; set; }
    public bool IsActive { get; set; } = true;
}
