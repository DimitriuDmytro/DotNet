namespace Bookings.Bll.Services;

using AutoMapper;
using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;
using Bookings.Dal.Interfaces;
using Bookings.Domain.Constants;
using Bookings.Domain.Exceptions;

/// <summary>
/// Сервіс бізнес-логіки для реєстрації та перегляду фінансових платежів по записах.
/// </summary>
public class PaymentService : IPaymentService
{
    private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cash",
        "Card",
        "BankTransfer"
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PaymentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IReadOnlyList<PaymentDto>> GetByAppointmentIdAsync(long appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);
        if (appointment == null)
        {
            throw new NotFoundException($"Запис з ID #{appointmentId} не знайдено.");
        }

        var payments = await _unitOfWork.Payments.GetByAppointmentIdAsync(appointmentId, cancellationToken);
        return _mapper.Map<IReadOnlyList<PaymentDto>>(payments);
    }

    public async Task RegisterPaymentAsync(long appointmentId, RegisterPaymentDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Amount <= 0)
        {
            throw new ValidationException(nameof(dto.Amount), "Сума платежу повинна бути більшою за нуль.");
        }

        if (string.IsNullOrWhiteSpace(dto.Method) || !AllowedMethods.Contains(dto.Method))
        {
            throw new ValidationException(nameof(dto.Method), "Некоректний метод оплати. Допустимі значення: Cash, Card, BankTransfer.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
            {
                throw new NotFoundException($"Запис з ID #{appointmentId} не знайдено.");
            }

            if (string.Equals(appointment.Status, AppointmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(appointment.Status, AppointmentStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessConflictException(
                    $"Неможливо зареєструвати оплату для запису #{appointmentId} у статусі [{appointment.Status}].");
            }

            var alreadyPaid = await _unitOfWork.Payments.GetTotalPaidForAppointmentAsync(appointmentId, cancellationToken);
            if (alreadyPaid + dto.Amount > appointment.TotalAmount)
            {
                throw new BusinessConflictException(
                    $"Сума платежу ({dto.Amount:F2}) перевищує залишок до сплати ({appointment.TotalAmount - alreadyPaid:F2}).");
            }

            await _unitOfWork.Payments.RegisterPaymentViaProcedureAsync(
                appointmentId, dto.Amount, dto.Method.Trim(), cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
