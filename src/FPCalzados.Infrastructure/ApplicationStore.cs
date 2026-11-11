using FPCalzados.Domain;

namespace FPCalzados.Infrastructure;

public sealed partial class ApplicationStore
{
    private readonly AppDbContextFactory factory;
    private readonly PricingService pricingService;
    private readonly IClock clock;
    private readonly SettingsApplicationService settings;
    private readonly SupplierApplicationService suppliers;
    private readonly SalesApplicationService sales;
    private readonly InventoryApplicationService inventory;
    private readonly DashboardReportApplicationService dashboardReports;
    private readonly AuditApplicationService audit;
    private UserRole? currentRole;

    public ApplicationStore(
        AppDbContextFactory factory,
        PricingService pricingService,
        IClock clock)
    {
        this.factory = factory;
        this.pricingService = pricingService;
        this.clock = clock;
        settings = new SettingsApplicationService(factory, clock);
        suppliers = new SupplierApplicationService(factory, clock);
        sales = new SalesApplicationService(factory, pricingService, clock);
        inventory = new InventoryApplicationService(factory, pricingService, clock);
        dashboardReports = new DashboardReportApplicationService(
            factory,
            clock,
            () => GetInventory(),
            (productId, variantId, take) => GetStockMovements(productId, variantId, take));
        audit = new AuditApplicationService(factory, clock);
    }

    private void RequireAdmin()
    {
        if (currentRole is not null && currentRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Esta acción requiere rol Administrador.");
        }
    }

    private void RecordAudit(string action, string entity, string? entityId, string message)
    {
        audit.Record(currentRole, action, entity, entityId, message);
    }
}

