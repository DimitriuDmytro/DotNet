namespace Bookings.Tests;

using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Bookings.Bll.DTOs;
using Bookings.Bll.Mapping;
using Bookings.Bll.Services;
using Bookings.Dal.Interfaces;
using Bookings.Domain.Constants;
using Bookings.Domain.Exceptions;
using Bookings.Domain.Models;

/// <summary>
/// Модульні тести бізнес-логіки AppointmentService з мокуванням IUnitOfWork через NSubstitute.
/// Перевіряють happy path зі знімком ServiceName/UnitPrice для 2 послуг, відкат транзакції (Rollback)
/// при неіснуючій послузі та матрицю переходів статусів.
/// </summary>
public class AppointmentServiceTests
{
    private readonly IUnitOfWork _uowMock;
    private readonly IClientRepository _clientRepoMock;
    private readonly IAppointmentRepository _appointmentRepoMock;
    private readonly IPaymentRepository _paymentRepoMock;
    private readonly ICatalogServiceRepository _catalogRepoMock;
    private readonly IMapper _mapper;
    private readonly AppointmentService _service;

    public AppointmentServiceTests()
    {
        _uowMock = Substitute.For<IUnitOfWork>();
        _clientRepoMock = Substitute.For<IClientRepository>();
        _appointmentRepoMock = Substitute.For<IAppointmentRepository>();
        _paymentRepoMock = Substitute.For<IPaymentRepository>();
        _catalogRepoMock = Substitute.For<ICatalogServiceRepository>();

        _uowMock.Clients.Returns(_clientRepoMock);
        _uowMock.Appointments.Returns(_appointmentRepoMock);
        _uowMock.Payments.Returns(_paymentRepoMock);
        _uowMock.Services.Returns(_catalogRepoMock);
        _uowMock.CatalogServices.Returns(_catalogRepoMock);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<BookingProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _service = new AppointmentService(_uowMock, _mapper);
    }

    [Fact]
    public async Task CreateAsync_WithTwoServices_ShouldCopyServiceNameAndUnitPriceSnapshotCalculateTotalAmountAndCommit()
    {
        // Arrange: замовлення з 2 послугами
        // Послуга #1: Price = 5000, Quantity = 1, Discount = 200 -> 4800
        // Послуга #2: Price = 1500, Quantity = 2, Discount = 300 -> 2700
        // Очікуваний TotalAmount = 4800 + 2700 = 7500
        var dto = new CreateAppointmentDto
        {
            ClientId = 1,
            ScheduledAt = DateTime.UtcNow.AddDays(2),
            CarBrand = "Porsche",
            CarModel = "911",
            PlateNumber = "KA9111AA",
            Items = new List<CreateAppointmentItemDto>
            {
                new() { ServiceId = 1, Quantity = 1, Discount = 200m },
                new() { ServiceId = 2, Quantity = 2, Discount = 300m }
            }
        };

        _clientRepoMock.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Client { Id = 1, Email = "p@test.com", FullName = "Іван", Phone = "+380501234567" });

        _catalogRepoMock.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CatalogService>
            {
                new() { Id = 1, Name = "Керамічне покриття 9H", Price = 5000m, DurationMin = 240 },
                new() { Id = 2, Name = "Хімчистка салону", Price = 1500m, DurationMin = 180 }
            });

        IReadOnlyList<AppointmentItem>? capturedItems = null;
        decimal capturedTotalAmount = 0m;

        _appointmentRepoMock.CreateViaProcedureAsync(
            1,
            Arg.Any<DateTime>(),
            "Porsche",
            "911",
            "KA9111AA",
            Arg.Any<string?>(),
            Arg.Any<string>(),
            Arg.Do<decimal>(total => capturedTotalAmount = total),
            Arg.Do<IReadOnlyList<AppointmentItem>>(items => capturedItems = items),
            Arg.Any<CancellationToken>())
            .Returns(new CreateAppointmentResult(42L, 7500m, AppointmentStatuses.Booked));

        _appointmentRepoMock.GetDetailedByIdAsync(42L, Arg.Any<CancellationToken>())
            .Returns(new Appointment
            {
                Id = 42L,
                ClientId = 1,
                Status = AppointmentStatuses.Booked,
                TotalAmount = 7500m,
                Details = new AppointmentDetails { AppointmentId = 42L, CarBrand = "Porsche", CarModel = "911", PlateNumber = "KA9111AA" }
            });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42L, result.Id);
        Assert.Equal(7500m, result.TotalAmount);
        Assert.Equal(7500m, capturedTotalAmount);

        Assert.NotNull(capturedItems);
        Assert.Equal(2, capturedItems!.Count);

        Assert.Equal(1L, capturedItems[0].ServiceId);
        Assert.Equal("Керамічне покриття 9H", capturedItems[0].ServiceName);
        Assert.Equal(5000m, capturedItems[0].UnitPrice);
        Assert.Equal(1, capturedItems[0].Quantity);
        Assert.Equal(200m, capturedItems[0].Discount);

        Assert.Equal(2L, capturedItems[1].ServiceId);
        Assert.Equal("Хімчистка салону", capturedItems[1].ServiceName);
        Assert.Equal(1500m, capturedItems[1].UnitPrice);
        Assert.Equal(2, capturedItems[1].Quantity);
        Assert.Equal(300m, capturedItems[1].Discount);

        await _uowMock.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _uowMock.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uowMock.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenServiceDoesNotExist_ShouldRollbackTransactionAndThrowNotFoundException()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            ClientId = 1,
            ScheduledAt = DateTime.UtcNow.AddDays(2),
            CarBrand = "Audi",
            CarModel = "RS6",
            PlateNumber = "AA0006RS",
            Items = new List<CreateAppointmentItemDto>
            {
                new() { ServiceId = 999, Quantity = 1, Discount = 0m }
            }
        };

        _clientRepoMock.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Client { Id = 1, Email = "a@test.com", FullName = "Петро", Phone = "+380501234567" });

        _catalogRepoMock.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CatalogService>());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(dto));

        // Перевіряємо, що транзакція була відкочена (Rollback), а Commit НЕ викликався
        await _uowMock.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _uowMock.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uowMock.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatusAsync_InvalidTransition_ShouldThrowBusinessConflictExceptionAndNotCallDb()
    {
        // Arrange: спроба перескочити з Booked одразу в Delivered
        _appointmentRepoMock.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new Appointment { Id = 7, Status = AppointmentStatuses.Booked });

        var dto = new ChangeStatusDto { NewStatus = AppointmentStatuses.Delivered };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessConflictException>(() => _service.ChangeStatusAsync(7, dto));

        await _appointmentRepoMock.DidNotReceive()
            .ChangeStatusViaProcedureAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeliverCarAsync_WhenNotFullyPaid_ShouldThrowBusinessConflictException()
    {
        // Arrange: статус Ready, але сплачено 1000 із 3000
        _appointmentRepoMock.GetByIdAsync(12, Arg.Any<CancellationToken>())
            .Returns(new Appointment { Id = 12, Status = AppointmentStatuses.Ready, TotalAmount = 3000m });

        _paymentRepoMock.GetTotalPaidForAppointmentAsync(12, Arg.Any<CancellationToken>())
            .Returns(1000m);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessConflictException>(() => _service.DeliverCarAsync(12));

        await _appointmentRepoMock.DidNotReceive()
            .DeliverCarViaProcedureAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WhenAlreadyInProgress_ShouldThrowBusinessConflictException()
    {
        // Arrange: статус InProgress не дозволяє скасування
        _appointmentRepoMock.GetByIdAsync(15, Arg.Any<CancellationToken>())
            .Returns(new Appointment { Id = 15, Status = AppointmentStatuses.InProgress });

        // Act & Assert
        await Assert.ThrowsAsync<BusinessConflictException>(() => _service.CancelAsync(15));
    }
}
