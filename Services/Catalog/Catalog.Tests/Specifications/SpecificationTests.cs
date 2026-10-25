using Catalog.Dal;
using Catalog.Dal.Repositories;
using Catalog.Dal.Specifications;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Tests.Specifications;

public class SpecificationTests
{
    private CatalogDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: "CatalogSpecTests_" + Guid.NewGuid())
            .Options;

        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task ServicesFilterSpecification_FiltersByCategoryAndSearchText()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var category1 = new ServiceCategory("Полірування", "Опис", 1);
        var category2 = new ServiceCategory("Хімчистка", "Опис", 2);
        context.ServiceCategories.AddRange(category1, category2);
        await context.SaveChangesAsync();

        var s1 = new Service(category1.Id, "М'яке полірування", 3000m, 180);
        var s2 = new Service(category1.Id, "Глибоке полірування", 6000m, 360);
        var s3 = new Service(category2.Id, "Хімчистка салону", 4000m, 240);
        context.Services.AddRange(s1, s2, s3);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<Service>(context);

        // Act - шукаємо послуги в категорії 1 зі словом "Глибоке"
        var spec = new ServicesFilterSpecification(
            search: "Глибоке",
            categoryId: category1.Id,
            minPrice: null,
            maxPrice: null,
            sortBy: "price",
            sortDir: "asc",
            page: 1,
            pageSize: 10);

        var result = await repo.ListAsync(spec);

        // Assert
        Assert.Single(result);
        Assert.Equal("Глибоке полірування", result[0].Name);
    }

    [Fact]
    public async Task ServicesFilterSpecification_SortsByPriceDescending()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var cat = new ServiceCategory("Детейлінг", null, 1);
        context.ServiceCategories.Add(cat);
        await context.SaveChangesAsync();

        context.Services.AddRange(
            new Service(cat.Id, "Послуга А", 1000m, 60),
            new Service(cat.Id, "Послуга Б", 5000m, 120),
            new Service(cat.Id, "Послуга В", 2500m, 90));
        await context.SaveChangesAsync();

        var repo = new GenericRepository<Service>(context);

        // Act
        var spec = new ServicesFilterSpecification(
            search: null,
            categoryId: null,
            minPrice: null,
            maxPrice: null,
            sortBy: "price",
            sortDir: "desc",
            page: 1,
            pageSize: 10);

        var result = await repo.ListAsync(spec);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(5000m, result[0].Price);
        Assert.Equal(2500m, result[1].Price);
        Assert.Equal(1000m, result[2].Price);
    }

    [Fact]
    public async Task ServicesWithCategoryAndSpecSpecification_FiltersByMinimumWarranty()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var cat = new ServiceCategory("Захисні покриття", null, 1);
        context.ServiceCategories.Add(cat);
        await context.SaveChangesAsync();

        var s1 = new Service(cat.Id, "Віск", 1500m, 60, id: 1);
        s1.AttachSpec("Carnauba Wax", 3, "Віск");

        var s2 = new Service(cat.Id, "Кераміка", 8000m, 300, id: 2);
        s2.AttachSpec("Ceramic Coat", 12, "Кераміка");

        var s3 = new Service(cat.Id, "Графен", 14000m, 480, id: 3);
        s3.AttachSpec("Graphene Coat", 24, "Графен");

        context.Services.AddRange(s1, s2, s3);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<Service>(context);

        // Act - фільтр гарантії від 12 місяців
        var spec = new ServicesWithCategoryAndSpecSpecification(minWarrantyMonths: 12);
        var result = await repo.ListAsync(spec);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, s => s.Name == "Віск");
        Assert.Contains(result, s => s.Name == "Кераміка");
        Assert.Contains(result, s => s.Name == "Графен");
    }
}
