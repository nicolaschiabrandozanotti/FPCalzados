using FPCalzados.Domain;

namespace FPCalzados.App;

public sealed record CartLine(
    int VariantId,
    string Product,
    int Quantity,
    decimal ManualDiscount,
    int PaymentRuleId,
    string RuleName,
    PaymentMethod PaymentMethod,
    int Installments,
    decimal InstallmentAmount,
    decimal UnitTotal,
    decimal LineTotal);
