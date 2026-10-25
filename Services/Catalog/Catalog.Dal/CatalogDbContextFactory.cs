namespace Catalog.Dal;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Фабрика контексту для інструментів EF Core CLI під час проектування (migrations, scaffolding).
/// </summary>
public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        const string fallbackConnection = "Server=(localdb)\\MSSQLLocalDB;Database=DetailingCatalog;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(fallbackConnection, sql =>
        {
            sql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.GetName().Name);
        });

        return new CatalogDbContext(optionsBuilder.Options);
    }
}
