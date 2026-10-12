namespace Bookings.Bll.Services;

using AutoMapper;
using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;
using Bookings.Dal.Interfaces;
using Bookings.Domain.Constants;
using Bookings.Domain.Exceptions;

/// <summary>
/// Сервіс бізнес-логіки для управління записами на детейлінг (Appointment Aggregate).
/// Координує транзакції через IUnitOfWork, перевіряє бізнес-інваріанти та викликає збережені процедури.
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AppointmentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<AppointmentDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var appointment = await _unitOfWork.Appointments.GetDetailedByIdAsync(id, cancellationToken);
        if (appointment == null)
        {
            throw new NotFoundException($"Запис з ID #{id} не знайдено.");
        }

        return _mapper.Map<AppointmentDto>(appointment);
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetByClientIdAsync(
        long clientId,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (status != null && !AppointmentStatuses.All.Contains(status))
        {
            throw new ValidationException(nameof(status), $"Невідомий статус для фільтрації: '{status}'.");
        }

        var appointments = await _unitOfWork.Appointments.GetByClientIdAsync(
            clientId, status, pageNumber, pageSize, cancellationToken);

        return _mapper.Map<IReadOnlyList<AppointmentDto>>(appointments);
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        ValidateCreateDto(dto);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Перевірка існування клієнта (ADO.NET репозиторій в тій самій транзакції)
            var client = await _unitOfWork.Clients.GetByIdAsync(dto.ClientId, cancellationToken);
            if (client == null)
            {
                throw new NotFoundException($"Клієнта з ID #{dto.ClientId} не знайдено.");
            }

            // 2. Перевірка наявності всіх замовлених послуг у локальній репліці Services та валідація знижок
            foreach (var item in dto.Items)
            {
                var catalogService = await _unitOfWork.CatalogServices.GetByServiceIdAsync(item.ServiceId, cancellationToken);
                if (catalogService == null)
                {
                    throw new ValidationException(nameof(dto.Items), $"Послугу з ID #{item.ServiceId} не знайдено в локальному довіднику.");
                }

                if (item.Discount > catalogService.Price * item.Quantity)
                {
                    throw new ValidationException(nameof(dto.Items),
                        $"Сума знижки ({item.Discount}) перевищує вартість послуги #{item.ServiceId} ({catalogService.Price * item.Quantity}).");
                }
            }

            // 3. Створення запису, деталей авто, позицій (TVP) та початкового статусу через збережену процедуру
            var tvpItems = dto.Items.Select(i => (i.ServiceId, i.Quantity, i.Discount)).ToList();

            var (appointmentId, _, _) = await _unitOfWork.Appointments.CreateViaProcedureAsync(
                dto.ClientId,
                dto.ScheduledAt,
                dto.CarBrand.Trim(),
                dto.CarModel.Trim(),
                dto.PlateNumber.Trim(),
                dto.Comment?.Trim(),
                dto.CreatedBy ?? "system",
                tvpItems,
                cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            // Повертаємо повний агрегат створеного запису
            return await GetByIdAsync(appointmentId, cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ChangeStatusAsync(long id, ChangeStatusDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.NewStatus) || !AppointmentStatuses.All.Contains(dto.NewStatus))
        {
            throw new ValidationException(nameof(dto.NewStatus), $"Некоректний цільовий статус: '{dto.NewStatus}'.");
        }

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id, cancellationToken);
        if (appointment == null)
        {
            throw new NotFoundException($"Запис з ID #{id} не знайдено.");
        }

        if (!AppointmentStatuses.IsAllowedTransition(appointment.Status, dto.NewStatus))
        {
            throw new BusinessConflictException(
                $"Недопустимий перехід статусу для запису #{id}: з [{appointment.Status}] у [{dto.NewStatus}].");
        }

        await _unitOfWork.Appointments.ChangeStatusViaProcedureAsync(
            id, dto.NewStatus, dto.ChangedBy ?? "system", cancellationToken);
    }

    public async Task DeliverCarAsync(long id, string? changedBy = null, CancellationToken cancellationToken = default)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id, cancellationToken);
        if (appointment == null)
        {
            throw new NotFoundException($"Запис з ID #{id} не знайдено.");
        }

        if (!string.Equals(appointment.Status, AppointmentStatuses.Ready, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessConflictException(
                $"Видача авто неможлива: поточний статус запису #{id} є [{appointment.Status}], очікується [{AppointmentStatuses.Ready}].");
        }

        var totalPaid = await _unitOfWork.Payments.GetTotalPaidForAppointmentAsync(id, cancellationToken);
        if (totalPaid < appointment.TotalAmount)
        {
            throw new BusinessConflictException(
                $"Видача авто заборонена: запис #{id} оплачено не повністю (сплачено {totalPaid:F2} із {appointment.TotalAmount:F2}).");
        }

        await _unitOfWork.Appointments.DeliverCarViaProcedureAsync(id, changedBy ?? "system", cancellationToken);
    }

    public async Task CancelAsync(long id, string? changedBy = null, CancellationToken cancellationToken = default)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id, cancellationToken);
        if (appointment == null)
        {
            throw new NotFoundException($"Запис з ID #{id} не знайдено.");
        }

        if (!AppointmentStatuses.CanBeCancelled(appointment.Status))
        {
            throw new BusinessConflictException(
                $"Скасування запису #{id} заборонено зі статусу [{appointment.Status}]. Дозволено лише з Booked або CarReceived.");
        }

        await _unitOfWork.Appointments.CancelViaProcedureAsync(id, changedBy ?? "system", cancellationToken);
    }

    public async Task<IReadOnlyList<StatusHistoryDto>> GetStatusHistoryAsync(long id, CancellationToken cancellationToken = default)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id, cancellationToken);
        if (appointment == null)
        {
            throw new NotFoundException($"Запис з ID #{id} не знайдено.");
        }

        var history = await _unitOfWork.Appointments.GetStatusHistoryAsync(id, cancellationToken);
        return _mapper.Map<IReadOnlyList<StatusHistoryDto>>(history);
    }

    private static void ValidateCreateDto(CreateAppointmentDto dto)
    {
        if (dto.ClientId <= 0)
            throw new ValidationException(nameof(dto.ClientId), "Ідентифікатор клієнта повинен бути додатним числом.");

        if (string.IsNullOrWhiteSpace(dto.CarBrand))
            throw new ValidationException(nameof(dto.CarBrand), "Марка автомобіля обов'язкова.");

        if (string.IsNullOrWhiteSpace(dto.CarModel))
            throw new ValidationException(nameof(dto.CarModel), "Модель автомобіля обов'язкова.");

        if (string.IsNullOrWhiteSpace(dto.PlateNumber))
            throw new ValidationException(nameof(dto.PlateNumber), "Державний номер автомобіля обов'язковий.");

        if (dto.Items == null || dto.Items.Count == 0)
            throw new ValidationException(nameof(dto.Items), "Запис повинен містити щонайменше одну послугу.");

        var duplicateServices = dto.Items.GroupBy(x => x.ServiceId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateServices.Count > 0)
            throw new ValidationException(nameof(dto.Items), "Дублювання однієї й тієї ж послуги в межах одного запису не допускається (збільшіть Quantity).");

        foreach (var item in dto.Items)
        {
            if (item.ServiceId <= 0)
                throw new ValidationException(nameof(dto.Items), "Ідентифікатор послуги повинен бути додатним.");
            if (item.Quantity <= 0)
                throw new ValidationException(nameof(dto.Items), "Кількість одиниць послуги повинна бути більшою за 0.");
            if (item.Discount < 0)
                throw new ValidationException(nameof(dto.Items), "Знижка не може бути від'ємною.");
        }
    }
}
