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
/// Модульні тести PaymentService (реєстрація оплат та відкат при переплаті).
/// </summary>
public class PaymentServiceTests
{
    private readonly IUnitOfWork _uowMock;
    private readonly IAppointmentRepository _appointmentRepoMock;
    private readonly IPaymentRepository _paymentRepoMock;
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _uowMock = Substitute.For<IUnitOfWork>();
        _appointmentRepoMock = Substitute.For<IAppointmentRepository>();
        _paymentRepoMock = Substitute.For<IPaymentRepository>();

        _uowMock.Appointments.Returns(_appointmentRepoMock);
        _uowMock.Payments.Returns(_paymentRepoMock);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<BookingProfile>(), NullLoggerFactory.Instance);
        _service = new PaymentService(_uowMock, config.CreateMapper());
    }

    [Fact]
    public async Task RegisterPaymentAsync_ValidPayment_ShouldCommitTransaction()
    {
        // Arrange
        _appointmentRepoMock.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(new Appointment { Id = 10, Status = AppointmentStatuses.InProgress, TotalAmount = 2000m });

        _paymentRepoMock.GetTotalPaidForAppointmentAsync(10, Arg.Any<CancellationToken>())
            .Returns(500m);

        var dto = new RegisterPaymentDto { Amount = 1500m, Method = "Card" };

        // Act
        await _service.RegisterPaymentAsync(10, dto);

        // Assert
        await _uowMock.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _paymentRepoMock.Received(1).RegisterPaymentViaProcedureAsync(10, 1500m, "Card", Arg.Any<CancellationToken>());
        await _uowMock.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uowMock.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterPaymentAsync_Overpayment_ShouldRollbackAndThrowBusinessConflictException()
    {
        // Arrange: вже сплачено 1800 із 2000, спроба сплатити ще 500
        _appointmentRepoMock.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(new Appointment { Id = 10, Status = AppointmentStatuses.InProgress, TotalAmount = 2000m });

        _paymentRepoMock.GetTotalPaidForAppointmentAsync(10, Arg.Any<CancellationToken>())
            .Returns(1800m);

        var dto = new RegisterPaymentDto { Amount = 500m, Method = "Cash" };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessConflictException>(() => _service.RegisterPaymentAsync(10, dto));

        await _uowMock.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _uowMock.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uowMock.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
