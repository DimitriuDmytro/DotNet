namespace Catalog.Dal;

using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Entities;

public class CatalogDbContext : DbContext
{
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceSpec> ServiceSpecs => Set<ServiceSpec>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageService> PackageServices => Set<PackageService>();
    public DbSet<ServiceImage> ServiceImages => Set<ServiceImage>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Критерій 2: підключення всіх IEntityTypeConfiguration через ApplyConfigurationsFromAssembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
