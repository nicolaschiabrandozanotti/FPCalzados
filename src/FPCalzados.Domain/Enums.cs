namespace FPCalzados.Domain;

public enum PaymentMethod
{
    Cash = 1,
    Debit = 2,
    Credit = 3
}

public enum SaleStatus
{
    Open = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Returned = 4,
    Cancelled = 5
}

public enum StockMovementType
{
    Purchase = 1,
    Sale = 2,
    Return = 3,
    Adjustment = 4,
    Import = 5
}

public enum AlertType
{
    LowStock = 1,
    OverduePayment = 2
}

public enum UserRole
{
    Admin = 1,
    Seller = 2
}
