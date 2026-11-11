using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public sealed record InventoryRow(
    int ProductId,
    int VariantId,
    string Brand,
    string Model,
    string Color,
    string Size,
    string? Sku,
    int StockQuantity,
    int LowStockThreshold,
    decimal SupplierCost,
    decimal CashPrice,
    bool IsActive)
{
    public string ProductGroup => $"{Brand} {Model}".Trim();
    public bool HasOptionDetail => !string.IsNullOrWhiteSpace(Color) || !string.IsNullOrWhiteSpace(Size);
    public bool IsLowStock => StockQuantity <= LowStockThreshold;
    public string StockStatus => IsLowStock ? "Bajo" : "OK";
    public string DisplayName => string.Join(" ", new[] { Brand, Model, Color, Size }.Where(item => !string.IsNullOrWhiteSpace(item)));
    public string OptionLabel
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Color))
            {
                parts.Add($"Color: {Color}");
            }

            if (!string.IsNullOrWhiteSpace(Size))
            {
                parts.Add($"Talle: {Size}");
            }

            return parts.Count == 0 ? "Sin color/talle" : string.Join(" | ", parts);
        }
    }
}

public sealed record SupplierRow(int Id, string Name, string Brand, bool IsActive)
{
    public string DisplayName => $"{Name} -> {Brand}";
}

public sealed record SupplierEditRequest(int? Id, string Name, string Brand, bool IsActive);

public sealed record ProductEditRequest(
    int? ProductId,
    int? VariantId,
    string Brand,
    string Model,
    string Color,
    string Size,
    string? Sku,
    int StockQuantity,
    int LowStockThreshold,
    decimal SupplierCost,
    decimal CashPrice);

public sealed record ProductStockEditRequest(
    int? ProductId,
    string Brand,
    string Model,
    int LowStockThreshold,
    decimal? SupplierCost,
    decimal CashPrice,
    IReadOnlyList<ProductVariantStockEditRequest> Variants);

public sealed record ProductVariantStockEditRequest(
    int? VariantId,
    string Color,
    string Size,
    int StockQuantity,
    string? Sku);

public sealed record SaleLineDraft(
    int VariantId,
    int Quantity,
    decimal ManualDiscount,
    int PaymentRuleId);

public sealed record SaleRow(
    int SaleId,
    DateTimeOffset CreatedAt,
    string Customer,
    DateOnly? DueDate,
    decimal GrossTotal,
    decimal NetTotal,
    decimal PaidTotal,
    decimal BalanceDue,
    SaleStatus Status);

public sealed record CustomerDuplicateRow(
    int CustomerId,
    string Name,
    string Phone,
    string? DocumentNumber,
    int SalesCount);

public sealed record CustomerDebtRow(
    string Customer,
    string Phone,
    int PendingSalesCount,
    decimal TotalPending,
    DateOnly? NextDueDate,
    string Status);

public sealed record CustomerHistory(
    string Customer,
    string Phone,
    decimal TotalPurchased,
    decimal TotalPaid,
    decimal TotalReturned,
    decimal BalanceDue,
    IReadOnlyList<CustomerHistorySaleRow> Sales,
    IReadOnlyList<CustomerHistoryPaymentRow> Payments,
    IReadOnlyList<CustomerHistoryReturnRow> Returns);

public sealed record CustomerHistorySaleRow(
    int SaleId,
    DateTimeOffset CreatedAt,
    DateOnly? DueDate,
    decimal NetTotal,
    decimal PaidTotal,
    decimal BalanceDue,
    SaleStatus Status);

public sealed record CustomerHistoryPaymentRow(
    int SaleId,
    DateTimeOffset PaidAt,
    PaymentMethod Method,
    decimal Amount,
    int Installments);

public sealed record CustomerHistoryReturnRow(
    int SaleId,
    DateTimeOffset CreatedAt,
    int Quantity,
    decimal Amount,
    decimal CashToReturn,
    string Reason);

public sealed record SaleDetail(
    int SaleId,
    DateTimeOffset CreatedAt,
    string Customer,
    string Phone,
    DateOnly? DueDate,
    SaleStatus Status,
    decimal GrossTotal,
    decimal ReturnedTotal,
    decimal NetTotal,
    decimal PaidTotal,
    decimal BalanceDue,
    decimal CashToReturn,
    IReadOnlyList<SaleDetailItemRow> Items,
    IReadOnlyList<SaleDetailPaymentRow> Payments,
    IReadOnlyList<SaleDetailReturnRow> Returns);

public sealed record SaleDetailItemRow(
    string Product,
    string Color,
    string Size,
    int Quantity,
    int ReturnedQuantity,
    decimal UnitPrice,
    decimal ManualDiscount,
    decimal FinalUnitPrice,
    decimal LineTotal);

public sealed record SaleDetailPaymentRow(
    DateTimeOffset PaidAt,
    PaymentMethod Method,
    decimal Amount,
    int Installments);

public sealed record SaleDetailReturnRow(
    DateTimeOffset CreatedAt,
    int Quantity,
    decimal Amount,
    decimal CashToReturn,
    string Reason);

public sealed record PaymentCollectionRow(
    int SaleId,
    DateTimeOffset PaidAt,
    string Customer,
    PaymentMethod Method,
    decimal Amount,
    int Installments,
    decimal SaleBalanceDue);

public sealed record DashboardSummary(
    decimal GrossSales,
    decimal NetSales,
    decimal PaidTotal,
    decimal PendingTotal,
    decimal ReturnedTotal,
    int LowStockCount,
    int OverdueCount,
    IReadOnlyList<TopProductRow> TopProducts,
    IReadOnlyList<AppAlert> Alerts);

public sealed record TopProductRow(string Brand, string Model, int Quantity, decimal NetTotal);

public sealed record ReturnableSaleItemRow(
    int SaleItemId,
    string Product,
    string Color,
    string Size,
    int Quantity,
    int ReturnedQuantity,
    decimal FinalUnitPrice)
{
    public int PendingReturnQuantity => Quantity - ReturnedQuantity;
}

public sealed record PriceOverrideRow(int Id, string? Color, string? Size, decimal Price);

public sealed record StockMovementRow(
    int Id,
    DateTimeOffset OccurredAt,
    string Brand,
    string Model,
    string Color,
    string Size,
    string? Sku,
    StockMovementType Type,
    int QuantityChange,
    int StockAfter,
    string Reason);

public sealed record CashRegisterSummary(
    int SalesCount,
    decimal GrossSales,
    decimal NetSales,
    decimal CashTotal,
    decimal DebitTotal,
    decimal CreditTotal,
    decimal PaidTotal,
    decimal PendingTotal,
    decimal ReturnedTotal,
    decimal CashToReturn,
    int? SessionId = null,
    DateTimeOffset? OpenedAt = null,
    DateTimeOffset? ClosedAt = null,
    decimal OpeningCash = 0m,
    decimal? ClosingCash = null,
    string? SessionNotes = null)
{
    public bool HasSession => SessionId is not null;
    public bool IsOpen => HasSession && ClosedAt is null;
    public decimal CashMovement => CashTotal - CashToReturn;
    public decimal ExpectedCash => OpeningCash + CashMovement;
    public decimal? Difference => ClosingCash is null ? null : ClosingCash.Value - ExpectedCash;
}

public sealed record ProfitabilitySummary(
    decimal NetSales,
    decimal EstimatedCost,
    decimal GrossProfit,
    decimal MarginPercent,
    IReadOnlyList<ProfitabilityProductRow> Products);

public sealed record ProfitabilityProductRow(
    string Brand,
    string Model,
    int Quantity,
    decimal NetSales,
    decimal EstimatedCost,
    decimal GrossProfit,
    decimal MarginPercent);

public sealed record AuditLogRow(
    int Id,
    DateTimeOffset OccurredAt,
    UserRole? Role,
    string Action,
    string Entity,
    string? EntityId,
    string Message);
