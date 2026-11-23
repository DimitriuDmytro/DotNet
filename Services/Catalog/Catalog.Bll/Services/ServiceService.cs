namespace Catalog.Bll.Services;

using System.Diagnostics;
using AutoMapper;
using Catalog.Bll.DTOs;
using Catalog.Bll.Interfaces;
using Catalog.Dal.Interfaces;
using Catalog.Dal.Specifications;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.EventBus.Events;
using Platform.EventBus.Publisher;
using Platform.EventBus.Topology;

public class ServiceService : IServiceService
{
    private static readonly ActivitySource Source = new("Catalog.Bll");
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ServiceService> _logger;
    private readonly IEventPublisher? _eventPublisher;

    public ServiceService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ServiceService>? logger = null,
        IEventPublisher? eventPublisher = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? NullLogger<ServiceService>.Instance;
        _eventPublisher = eventPublisher;
    }

    public async Task<PagedResult<ServiceDto>> GetPagedAsync(ServiceQuery query, CancellationToken cancellationToken = default)
    {
        // Критерій 10 & 14: пагінація та фільтрація через специфікації на рівні SQL
        var filterSpec = new ServicesFilterSpecification(
            query.Search,
            query.CategoryId,
            query.MinPrice,
            query.MaxPrice,
            query.SortBy,
            query.SortDir,
            query.Page,
            query.PageSize);

        var countSpec = new ServicesFilterCountSpecification(
            query.Search,
            query.CategoryId,
            query.MinPrice,
            query.MaxPrice);

        var items = await _unitOfWork.Services.ListAsync(filterSpec, cancellationToken);
        var totalCount = await _unitOfWork.Services.CountAsync(countSpec, cancellationToken);

        var dtos = _mapper.Map<IReadOnlyList<ServiceDto>>(items);
        return new PagedResult<ServiceDto>(dtos, totalCount, query.Page, query.PageSize);
    }

    public async Task<ServiceDetailDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        // Критерій 6.1 (Eager loading - 1 SQL-запит)
        var service = await _unitOfWork.Services.GetWithDetailsAsync(id, cancellationToken);
        if (service == null)
        {
            throw new EntityNotFoundException(nameof(Service), id);
        }

        return _mapper.Map<ServiceDetailDto>(service);
    }

    public async Task<ServiceDetailDto> GetByIdExplicitAsync(long id, CancellationToken cancellationToken = default)
    {
        // Критерій 6.2 (Explicit loading - 2+ SQL-запити)
        var service = await _unitOfWork.Services.GetWithExplicitDetailsAsync(id, cancellationToken);
        if (service == null)
        {
            throw new EntityNotFoundException(nameof(Service), id);
        }

        return _mapper.Map<ServiceDetailDto>(service);
    }

    public async Task<IReadOnlyList<ServiceDto>> GetWithMinWarrantyAsync(int minWarrantyMonths, CancellationToken cancellationToken = default)
    {
        // Критерій 14: застосування другої специфікації через той самий GenericRepository.ListAsync без змін у коді репозиторію
        var spec = new ServicesWithCategoryAndSpecSpecification(minWarrantyMonths);
        var services = await _unitOfWork.Services.ListAsync(spec, cancellationToken);
        return _mapper.Map<IReadOnlyList<ServiceDto>>(services);
    }

    public async Task<ServiceDetailDto> CreateAsync(CreateServiceDto dto, CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("CreateCatalogService");
        activity?.SetTag("service.name", dto.Name);
        activity?.SetTag("category.id", dto.CategoryId);
        activity?.SetTag("service.price", dto.Price);

        _logger.LogInformation("Створення нової послуги каталогу '{ServiceName}' (категорія #{CategoryId}, ціна {Price} грн)",
            dto.Name, dto.CategoryId, dto.Price);

        if (await _unitOfWork.Services.ExistsByNameAsync(dto.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"Послуга з назвою '{dto.Name}' уже існує в каталозі.");
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new EntityNotFoundException(nameof(ServiceCategory), dto.CategoryId);
        }

        var service = new Service(
            dto.CategoryId,
            dto.Name,
            dto.Price,
            dto.DurationMin,
            dto.CreatedBy ?? "system");

        if (dto.Spec != null)
        {
            service.AttachSpec(dto.Spec.Materials, dto.Spec.WarrantyMonths, dto.Spec.Description);
        }

        if (!string.IsNullOrWhiteSpace(dto.MainImageUrl))
        {
            service.AddImage(dto.MainImageUrl.Trim(), isMain: true);
        }

        await _unitOfWork.Services.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        activity?.SetTag("service.id", service.Id);
        _logger.LogInformation("Послугу #{ServiceId} '{ServiceName}' успішно створено", service.Id, service.Name);

        if (_eventPublisher != null)
        {
            try
            {
                await _eventPublisher.PublishAsync(
                    EventBusTopology.CatalogEventsExchange,
                    EventBusTopology.ProductCreatedRoutingKey,
                    new ProductCreatedEvent(service.Id, service.Name, service.Price, service.DurationMin, DateTime.UtcNow, Activity.Current?.Id),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка публікації ProductCreatedEvent для послуги #{ServiceId}", service.Id);
            }
        }

        return await GetByIdAsync(service.Id, cancellationToken);
    }

    public async Task<ServiceDetailDto> UpdateAsync(long id, UpdateServiceDto dto, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service == null)
        {
            throw new EntityNotFoundException(nameof(Service), id);
        }

        if (await _unitOfWork.Services.ExistsByNameAsync(dto.Name, excludeId: id, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"Інша послуга з назвою '{dto.Name}' уже існує.");
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new EntityNotFoundException(nameof(ServiceCategory), dto.CategoryId);
        }

        var oldName = service.Name;
        service.UpdateDetails(dto.Name, dto.CategoryId, dto.DurationMin, dto.UpdatedBy);
        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (_eventPublisher != null && !string.Equals(oldName, dto.Name, StringComparison.Ordinal))
        {
            try
            {
                await _eventPublisher.PublishAsync(
                    EventBusTopology.CatalogEventsExchange,
                    EventBusTopology.ProductRenamedRoutingKey,
                    new ProductRenamedEvent(service.Id, oldName, dto.Name, DateTime.UtcNow, Activity.Current?.Id),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка публікації ProductRenamedEvent для послуги #{ServiceId}", service.Id);
            }
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ServiceDetailDto> UpdatePriceAsync(long id, UpdatePriceDto dto, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service == null)
        {
            throw new EntityNotFoundException(nameof(Service), id);
        }

        var oldPrice = service.Price;
        service.UpdatePrice(dto.NewPrice, dto.UpdatedBy);
        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (_eventPublisher != null)
        {
            try
            {
                await _eventPublisher.PublishAsync(
                    EventBusTopology.CatalogEventsExchange,
                    EventBusTopology.ProductPriceChangedRoutingKey,
                    new ProductPriceChangedEvent(service.Id, service.Name, oldPrice, dto.NewPrice, DateTime.UtcNow, Activity.Current?.Id),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка публікації ProductPriceChangedEvent для послуги #{ServiceId}", service.Id);
            }
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id, cancellationToken);
        if (service == null)
        {
            throw new EntityNotFoundException(nameof(Service), id);
        }

        var serviceName = service.Name;
        service.SoftDelete("system");
        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (_eventPublisher != null)
        {
            try
            {
                await _eventPublisher.PublishAsync(
                    EventBusTopology.CatalogEventsExchange,
                    EventBusTopology.ProductDeletedRoutingKey,
                    new ProductDeletedEvent(service.Id, serviceName, DateTime.UtcNow, Activity.Current?.Id),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка публікації ProductDeletedEvent для послуги #{ServiceId}", service.Id);
            }
        }
    }

    public async Task<ServiceImageDto> AddImageAsync(long serviceId, AddServiceImageDto dto, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetWithDetailsAsync(serviceId, cancellationToken);
        if (service == null)
        {
            throw new EntityNotFoundException(nameof(Service), serviceId);
        }

        var image = service.AddImage(dto.Url, dto.IsMain);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ServiceImageDto>(image);
    }

    public async Task RemoveImageAsync(long serviceId, long imageId, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetWithDetailsAsync(serviceId, cancellationToken);
        if (service == null)
        {
            throw new EntityNotFoundException(nameof(Service), serviceId);
        }

        service.RemoveImage(imageId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ServiceSpecDto> UpsertSpecAsync(long serviceId, UpsertServiceSpecDto dto, CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services.GetWithDetailsAsync(serviceId, cancellationToken);
        if (service == null)
        {
            throw new EntityNotFoundException(nameof(Service), serviceId);
        }

        service.AttachSpec(dto.Materials, dto.WarrantyMonths, dto.Description);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ServiceSpecDto>(service.Spec);
    }
}
