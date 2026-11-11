using Microsoft.EntityFrameworkCore.Design;

namespace FPCalzados.Infrastructure;

public sealed class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "FPCalzados-design.sqlite");
        return new AppDbContextFactory(databasePath).CreateDbContext();
    }
}
