using FPCalzados.Domain;

namespace FPCalzados.App;

public sealed record PaymentLine(PaymentMethod Method, decimal Amount, int Installments);
