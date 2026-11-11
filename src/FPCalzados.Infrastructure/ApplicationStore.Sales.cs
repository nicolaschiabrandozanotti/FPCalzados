using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

public sealed partial class ApplicationStore
{
    public Sale CreateSale(
        IReadOnlyList<SaleLineDraft> lines,
        IReadOnlyList<PaymentRequest> payments,
        Customer? customer,
        DateOnly? dueDate)
    {
        return sales.CreateSale(lines, payments, customer, dueDate, currentRole);
    }

    public IReadOnlyList<SaleRow> GetSales(DateOnly from, DateOnly to)
    {
        return sales.GetSales(from, to);
    }

    public PagedResult<SaleRow> GetSalesPage(DateOnly from, DateOnly to, string? search, int page, int pageSize = 100)
    {
        return sales.GetSalesPage(from, to, search, page, pageSize);
    }

    public SaleDetail GetSaleDetail(int saleId)
    {
        return sales.GetSaleDetail(saleId);
    }

    public IReadOnlyList<PaymentCollectionRow> GetPaymentCollections(DateOnly from, DateOnly to)
    {
        return sales.GetPaymentCollections(from, to);
    }

    public string ExportPaymentCollectionsReport(string directory, DateOnly from, DateOnly to)
    {
        return sales.ExportPaymentCollectionsReport(directory, from, to);
    }

    public IReadOnlyList<CustomerDuplicateRow> GetPotentialCustomerDuplicates()
    {
        RequireAdmin();
        return sales.GetPotentialCustomerDuplicates();
    }

    public void MergeCustomers(int targetCustomerId, IReadOnlyCollection<int> sourceCustomerIds)
    {
        RequireAdmin();
        sales.MergeCustomers(targetCustomerId, sourceCustomerIds, currentRole);
    }

    public IReadOnlyList<CustomerDebtRow> GetCustomerDebts()
    {
        return sales.GetCustomerDebts();
    }

    public CustomerHistory GetCustomerHistory(string customer, string phone)
    {
        return sales.GetCustomerHistory(customer, phone);
    }

    public string ExportCustomerDebtReport(string directory)
    {
        return sales.ExportCustomerDebtReport(directory);
    }

    public string ExportSaleDetailReport(int saleId, string directory)
    {
        return sales.ExportSaleDetailReport(saleId, directory);
    }

    public string BuildSaleReceiptText(int saleId)
    {
        return sales.BuildSaleReceiptText(saleId);
    }

    public string ExportSaleReceiptText(int saleId, string directory)
    {
        return sales.ExportSaleReceiptText(saleId, directory);
    }

    public Payment RegisterPayment(int saleId, PaymentRequest request)
    {
        return sales.RegisterPayment(saleId, request, currentRole);
    }

    public void UpdateSaleDueDate(int saleId, DateOnly? dueDate)
    {
        sales.UpdateSaleDueDate(saleId, dueDate, currentRole);
    }

    public ReturnRecord ReturnSaleItem(int saleId, int saleItemId, int quantity, string reason)
    {
        return sales.ReturnSaleItem(saleId, saleItemId, quantity, reason, currentRole);
    }

    public IReadOnlyList<ReturnableSaleItemRow> GetReturnableSaleItems(int saleId)
    {
        return sales.GetReturnableSaleItems(saleId);
    }
}
