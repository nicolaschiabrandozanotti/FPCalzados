using Microsoft.EntityFrameworkCore;

namespace FPCalzados.Infrastructure;

public sealed class AppDbContextFactory(string databasePath)
{
    public string DatabasePath { get; } = databasePath;

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={DatabasePath};Pooling=False")
            .Options;

        return new AppDbContext(options);
    }

}
