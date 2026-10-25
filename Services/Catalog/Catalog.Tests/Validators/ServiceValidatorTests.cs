using Catalog.Bll.DTOs;
using Catalog.Bll.Validators;

namespace Catalog.Tests.Validators;

public class ServiceValidatorTests
{
    private readonly CreateServiceDtoValidator _createValidator = new();
    private readonly UpdatePriceDtoValidator _priceValidator = new();

    [Fact]
    public async Task CreateServiceDtoValidator_WithValidDto_PassesValidation()
    {
        // Arrange
        var dto = new CreateServiceDto
        {
            CategoryId = 1,
            Name = "Відновлювальне полірування кузова",
            Price = 6500m,
            DurationMin = 360
        };

        // Act
        var result = await _createValidator.ValidateAsync(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public async Task CreateServiceDtoValidator_WithInvalidName_FailsValidation(string name)
    {
        // Arrange
        var dto = new CreateServiceDto
        {
            CategoryId = 1,
            Name = name,
            Price = 1000m,
            DurationMin = 60
        };

        // Act
        var result = await _createValidator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateServiceDto.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task CreateServiceDtoValidator_WithInvalidPrice_FailsValidation(decimal price)
    {
        // Arrange
        var dto = new CreateServiceDto
        {
            CategoryId = 1,
            Name = "Хімчистка дисків",
            Price = price,
            DurationMin = 30
        };

        // Act
        var result = await _createValidator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateServiceDto.Price));
    }

    [Fact]
    public async Task UpdatePriceDtoValidator_WithPositivePrice_PassesValidation()
    {
        // Arrange
        var dto = new UpdatePriceDto { NewPrice = 2500m };

        // Act
        var result = await _priceValidator.ValidateAsync(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task UpdatePriceDtoValidator_WithNonPositivePrice_FailsValidation(decimal price)
    {
        // Arrange
        var dto = new UpdatePriceDto { NewPrice = price };

        // Act
        var result = await _priceValidator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePriceDto.NewPrice));
    }
}
