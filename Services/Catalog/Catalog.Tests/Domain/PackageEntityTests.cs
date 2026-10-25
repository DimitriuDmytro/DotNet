using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Tests.Domain;

public class PackageEntityTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInstance()
    {
        // Act
        var package = new Package("Преміум детейлінг", 15000m, "Повний пакет для нового авто");

        // Assert
        Assert.Equal("Преміум детейлінг", package.Name);
        Assert.Equal(15000m, package.Price);
        Assert.False(package.IsDeleted);
        Assert.Empty(package.PackageServices);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Constructor_WithInvalidPrice_ThrowsDomainException(decimal invalidPrice)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Package("Пакет", invalidPrice, "Опис"));

        Assert.Contains("більшою за", ex.Message);
    }

    [Fact]
    public void AddService_WithValidServiceId_AddsToPackage()
    {
        // Arrange
        var package = new Package("Передпродажна підготовка", 8000m, null);

        // Act
        var ps = package.AddService(serviceId: 5, sortOrder: 1);

        // Assert
        Assert.Single(package.PackageServices);
        Assert.Equal(5, ps.ServiceId);
        Assert.Equal(1, ps.SortOrder);
    }

    [Fact]
    public void AddService_WhenAddingDuplicateServiceId_ThrowsDomainException()
    {
        // Arrange
        var package = new Package("Передпродажна підготовка", 8000m, null);
        package.AddService(serviceId: 5, sortOrder: 1);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => package.AddService(serviceId: 5, sortOrder: 2));
        Assert.Contains("вже додана до цього пакета", ex.Message);
    }

    [Fact]
    public void RemoveService_WithExistingServiceId_RemovesSuccessfully()
    {
        // Arrange
        var package = new Package("Передпродажна підготовка", 8000m, null);
        package.AddService(serviceId: 5, sortOrder: 1);

        // Act
        package.RemoveService(serviceId: 5);

        // Assert
        Assert.Empty(package.PackageServices);
    }

    [Fact]
    public void RemoveService_WithNonExistingServiceId_ThrowsDomainException()
    {
        // Arrange
        var package = new Package("Передпродажна підготовка", 8000m, null);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => package.RemoveService(999));
        Assert.Contains("не знайдено у складі", ex.Message);
    }
}
