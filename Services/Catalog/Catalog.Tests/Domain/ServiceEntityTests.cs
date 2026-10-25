using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Tests.Domain;

public class ServiceEntityTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInstance()
    {
        // Act
        var service = new Service(1, "Комплексна хімчистка", 4500m, 360);

        // Assert
        Assert.Equal(1, service.CategoryId);
        Assert.Equal("Комплексна хімчистка", service.Name);
        Assert.Equal(4500m, service.Price);
        Assert.Equal(360, service.DurationMin);
        Assert.False(service.IsDeleted);
        Assert.Empty(service.Images);
        Assert.Null(service.Spec);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsDomainException(string emptyName)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Service(1, emptyName, 1000m, 60));

        Assert.Contains("порожньою", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Constructor_WithNonPositivePrice_ThrowsDomainException(decimal invalidPrice)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Service(1, "Полірування", invalidPrice, 60));

        Assert.Contains("більшою за", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Constructor_WithNonPositiveDuration_ThrowsDomainException(int invalidDuration)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Service(1, "Полірування", 2000m, invalidDuration));

        Assert.Contains("більшою за", ex.Message);
    }

    [Fact]
    public void UpdatePrice_WithValidPrice_UpdatesPriceAndTimestamp()
    {
        // Arrange
        var service = new Service(1, "Полірування фар", 800m, 60);

        // Act
        service.UpdatePrice(1200m);

        // Assert
        Assert.Equal(1200m, service.Price);
        Assert.NotNull(service.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void UpdatePrice_WithInvalidPrice_ThrowsDomainException(decimal invalidPrice)
    {
        // Arrange
        var service = new Service(1, "Полірування фар", 800m, 60);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => service.UpdatePrice(invalidPrice));
        Assert.Contains("більшою за", ex.Message);
    }

    [Fact]
    public void AttachSpec_WithValidSpec_AttachesSpec()
    {
        // Arrange
        var service = new Service(1, "Кераміка", 10000m, 480);

        // Act
        service.AttachSpec("Ceramic Pro", 12, "Чистий кузов");

        // Assert
        Assert.NotNull(service.Spec);
        Assert.Equal(12, service.Spec.WarrantyMonths);
        Assert.Equal("Ceramic Pro", service.Spec.Materials);
    }

    [Fact]
    public void AttachSpec_WhenSpecAlreadyExists_UpdatesExistingSpec()
    {
        // Arrange
        var service = new Service(1, "Кераміка", 10000m, 480);
        service.AttachSpec("Ceramic Pro", 12, "Чистий кузов");

        // Act
        service.AttachSpec("Ceramic Pro 9H", 24, "3 шари");

        // Assert
        Assert.NotNull(service.Spec);
        Assert.Equal(24, service.Spec.WarrantyMonths);
        Assert.Equal("Ceramic Pro 9H", service.Spec.Materials);
    }

    [Fact]
    public void AddImage_WhenAddingMainImage_UnsetsPreviousMainImage()
    {
        // Arrange
        var service = new Service(1, "Полірування кузова", 5000m, 240);

        // Act
        var img1 = service.AddImage("https://img/1.jpg", isMain: true);
        var img2 = service.AddImage("https://img/2.jpg", isMain: true);

        // Assert
        Assert.Equal(2, service.Images.Count);
        Assert.False(img1.IsMain);
        Assert.True(img2.IsMain);
    }

    [Fact]
    public void SoftDelete_And_Restore_TogglesStateCorrectly()
    {
        // Arrange
        var service = new Service(1, "Озонування", 600m, 30);

        // Act - Delete
        service.SoftDelete();
        Assert.True(service.IsDeleted);
        Assert.NotNull(service.UpdatedAt);

        // Act - Restore
        service.Restore();
        Assert.False(service.IsDeleted);
    }
}
