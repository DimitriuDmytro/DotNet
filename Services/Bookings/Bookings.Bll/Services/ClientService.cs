namespace Bookings.Bll.Services;

using AutoMapper;
using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;
using Bookings.Dal.Interfaces;
using Bookings.Domain.Exceptions;
using Bookings.Domain.Models;

/// <summary>
/// Сервіс управління клієнтами (робота через Unit of Work та ADO.NET репозиторій).
/// </summary>
public class ClientService : IClientService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ClientService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<ClientDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var client = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken);
        return client == null ? null : _mapper.Map<ClientDto>(client);
    }

    public async Task<ClientDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException(nameof(email), "Email клієнта не може бути порожнім.");

        var client = await _unitOfWork.Clients.GetByEmailAsync(email.Trim(), cancellationToken);
        return client == null ? null : _mapper.Map<ClientDto>(client);
    }

    public async Task<PagedResult<ClientDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var clients = await _unitOfWork.Clients.GetAllAsync(pageNumber, pageSize, cancellationToken);
        var totalCount = await _unitOfWork.Clients.GetCountAsync(cancellationToken);

        return new PagedResult<ClientDto>
        {
            Items = _mapper.Map<IReadOnlyList<ClientDto>>(clients),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ClientDto> CreateAsync(CreateClientDto dto, CancellationToken cancellationToken = default)
    {
        ValidateCreateDto(dto);

        // Перевірка унікальності email
        var existing = await _unitOfWork.Clients.GetByEmailAsync(dto.Email.Trim(), cancellationToken);
        if (existing != null)
        {
            throw new BusinessConflictException($"Клієнт з електронною поштою '{dto.Email}' вже зареєстрований.");
        }

        var client = _mapper.Map<Client>(dto);
        client.Email = dto.Email.Trim();
        client.CreatedBy = dto.CreatedBy ?? "system";

        var id = await _unitOfWork.Clients.AddAsync(client, cancellationToken);
        var created = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken);
        return _mapper.Map<ClientDto>(created!);
    }

    public async Task<ClientDto> UpdateAsync(long id, UpdateClientDto dto, CancellationToken cancellationToken = default)
    {
        ValidateUpdateDto(dto);

        var client = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken);
        if (client == null)
        {
            throw new NotFoundException($"Клієнта з ID #{id} не знайдено.");
        }

        // Перевірка зміни email на вже зайнятий іншим клієнтом
        var existingWithEmail = await _unitOfWork.Clients.GetByEmailAsync(dto.Email.Trim(), cancellationToken);
        if (existingWithEmail != null && existingWithEmail.Id != id)
        {
            throw new BusinessConflictException($"Електронна пошта '{dto.Email}' вже використовується іншим клієнтом.");
        }

        client.Email = dto.Email.Trim();
        client.FullName = dto.FullName.Trim();
        client.Phone = dto.Phone.Trim();
        client.UpdatedBy = dto.UpdatedBy ?? "system";

        var updated = await _unitOfWork.Clients.UpdateAsync(client, cancellationToken);
        if (!updated)
        {
            throw new BusinessConflictException($"Не вдалося оновити дані клієнта #{id}.");
        }

        var freshClient = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken);
        return _mapper.Map<ClientDto>(freshClient!);
    }

    public async Task DeleteAsync(long id, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        var client = await _unitOfWork.Clients.GetByIdAsync(id, cancellationToken);
        if (client == null)
        {
            throw new NotFoundException($"Клієнта з ID #{id} не знайдено.");
        }

        var deleted = await _unitOfWork.Clients.DeleteAsync(id, deletedBy ?? "system", cancellationToken);
        if (!deleted)
        {
            throw new BusinessConflictException($"Не вдалося видалити клієнта #{id}.");
        }
    }

    private static void ValidateCreateDto(CreateClientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains('@'))
            throw new ValidationException(nameof(dto.Email), "Вказано некоректну електронну пошту.");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new ValidationException(nameof(dto.FullName), "Повне ім'я клієнта обов'язкове.");

        if (string.IsNullOrWhiteSpace(dto.Phone))
            throw new ValidationException(nameof(dto.Phone), "Номер телефону клієнта обов'язковий.");
    }

    private static void ValidateUpdateDto(UpdateClientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains('@'))
            throw new ValidationException(nameof(dto.Email), "Вказано некоректну електронну пошту.");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new ValidationException(nameof(dto.FullName), "Повне ім'я клієнта обов'язкове.");

        if (string.IsNullOrWhiteSpace(dto.Phone))
            throw new ValidationException(nameof(dto.Phone), "Номер телефону клієнта обов'язковий.");
    }
}
