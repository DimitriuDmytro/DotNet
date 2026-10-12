namespace Bookings.Tests;

using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Bookings.Bll.DTOs;
using Bookings.Bll.Mapping;
using Bookings.Domain.Models;

/// <summary>
/// Тести конфігурації AutoMapper (BookingProfile).
/// Перевіряє відсутність незіставлених властивостей та коректність прямого і зворотного мапінгу.
/// </summary>
public class AutoMapperConfigurationTests
{
    private readonly MapperConfiguration _configuration;
    private readonly IMapper _mapper;

    public AutoMapperConfigurationTests()
    {
        _configuration = new MapperConfiguration(cfg => cfg.AddProfile<BookingProfile>(), NullLoggerFactory.Instance);
        _mapper = _configuration.CreateMapper();
    }

    [Fact]
    public void BookingProfile_Configuration_ShouldBeValid()
    {
        _configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_ClientToClientDto_ShouldMapAllProperties()
    {
        // Arrange
        var client = new Client
        {
            Id = 10,
            Email = "test@example.com",
            FullName = "Олександр Коваленко",
            Phone = "+380501112233",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };

        // Act
        var dto = _mapper.Map<ClientDto>(client);

        // Assert
        Assert.Equal(client.Id, dto.Id);
        Assert.Equal(client.Email, dto.Email);
        Assert.Equal(client.FullName, dto.FullName);
        Assert.Equal(client.Phone, dto.Phone);
        Assert.Equal(client.CreatedBy, dto.CreatedBy);
    }

    [Fact]
    public void Map_AppointmentWithDetailsAndItems_ShouldMapNestedObjects()
    {
        // Arrange
        var appointment = new Appointment
        {
            Id = 5,
            ClientId = 1,
            Status = "Booked",
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            TotalAmount = 4500.00m,
            Details = new AppointmentDetails
            {
                AppointmentId = 5,
                CarBrand = "BMW",
                CarModel = "M5",
                PlateNumber = "AA0001AA",
                Comment = "Керамічне покриття"
            },
            Items = new List<AppointmentItem>
            {
                new()
                {
                    AppointmentId = 5,
                    ServiceId = 101,
                    ServiceName = "Полірування кузова",
                    UnitPrice = 5000.00m,
                    Discount = 500.00m,
                    Quantity = 1
                }
            }
        };

        // Act
        var dto = _mapper.Map<AppointmentDto>(appointment);

        // Assert
        Assert.Equal(5, dto.Id);
        Assert.Equal(4500.00m, dto.TotalAmount);
        Assert.NotNull(dto.Details);
        Assert.Equal("BMW", dto.Details!.CarBrand);
        Assert.Single(dto.Items);
        Assert.Equal("Полірування кузова", dto.Items[0].ServiceName);
    }
}
