using AutoMapper;
using Catalog.Bll.DTOs;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Catalog.Tests.Services;

public class ServiceServiceTests
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ServiceService _service;

    public ServiceServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CatalogProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new ServiceService(_uow, _mapper);
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsUnique_CreatesAndReturnsDto()
    {
        // Arrange
        var dto = new CreateServiceDto
        {
            CategoryId = 1,
            Name = "Антидощ на всі стекла",
            Price = 1200m,
            DurationMin = 60
        };

        var category = new ServiceCategory("Захисні покриття", null, 1);
        var createdService = new Service(1, "Антидощ на всі стекла", 1200m, 60, id: 10);

        _uow.Categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);
        _uow.Services.ExistsByNameAsync("Антидощ на всі стекла", null, Arg.Any<CancellationToken>()).Returns(false);
        _uow.Services.GetWithDetailsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(createdService);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Антидощ на всі стекла", result.Name);
        Assert.Equal(1200m, result.Price);
        await _uow.Services.Received(1).AddAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
        await _uow.SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var dto = new CreateServiceDto
        {
            CategoryId = 1,
            Name = "Однокрокове полірування",
            Price = 3000m,
            DurationMin = 180
        };

        _uow.Services.ExistsByNameAsync("Однокрокове полірування", null, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task GetByIdAsync_WhenServiceDoesNotExist_ThrowsEntityNotFoundException()
    {
        // Arrange
        _uow.Services.GetWithDetailsAsync(999, Arg.Any<CancellationToken>()).Returns((Service?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task UpdatePriceAsync_WhenServiceExists_UpdatesPriceAndSaves()
    {
        // Arrange
        var service = new Service(1, "Тонування скла", 4000m, 180, id: 1);
        _uow.Services.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(service);
        _uow.Services.GetWithDetailsAsync(1, Arg.Any<CancellationToken>()).Returns(service);

        // Act
        var result = await _service.UpdatePriceAsync(1, new UpdatePriceDto { NewPrice = 4500m });

        // Assert
        Assert.Equal(4500m, result.Price);
        Assert.Equal(4500m, service.Price);
        await _uow.SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
