namespace Bookings.Bll.Services;

using AutoMapper;
using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;
using Bookings.Dal.Interfaces;
using Bookings.Domain.Exceptions;
using Bookings.Domain.Models;

/// <summary>
/// Сервіс для читання та синхронізації локальної Read-Model копії каталогу послуг (таблиця dbo.Services).
/// </summary>
public class CatalogQueryService : ICatalogQueryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CatalogQueryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IReadOnlyList<CatalogServiceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var services = await _unitOfWork.Services.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyList<CatalogServiceDto>>(services);
    }

    public async Task<CatalogServiceDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        return service == null ? null : _mapper.Map<CatalogServiceDto>(service);
    }

    public async Task UpsertAsync(UpsertCatalogServiceDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Id <= 0)
            throw new ValidationException(nameof(dto.Id), "Ідентифікатор послуги з Каталогу повинен бути додатним.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ValidationException(nameof(dto.Name), "Назва послуги обов'язкова.");

        if (dto.Price <= 0)
            throw new ValidationException(nameof(dto.Price), "Ціна послуги повинна бути більшою за нуль.");

        if (dto.DurationMin <= 0)
            throw new ValidationException(nameof(dto.DurationMin), "Тривалість послуги (хв) повинна бути більшою за нуль.");

        var entity = _mapper.Map<CatalogService>(dto);
        entity.Name = dto.Name.Trim();

        await _unitOfWork.Services.UpsertAsync(entity, cancellationToken);
    }
}
