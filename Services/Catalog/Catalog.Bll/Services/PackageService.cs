namespace Catalog.Bll.Services;

using AutoMapper;
using Catalog.Bll.DTOs;
using Catalog.Bll.Interfaces;
using Catalog.Dal.Interfaces;
using Catalog.Dal.Specifications;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

public class PackageService : IPackageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PackageService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<PagedResult<PackageDto>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new PackagesFilterSpecification(search, minPrice, maxPrice, page, pageSize);
        var countSpec = new PackagesFilterCountSpecification(search, minPrice, maxPrice);

        var packages = await _unitOfWork.Packages.ListAsync(spec, cancellationToken);
        var totalCount = await _unitOfWork.Packages.CountAsync(countSpec, cancellationToken);

        var dtos = _mapper.Map<IReadOnlyList<PackageDto>>(packages);
        return new PagedResult<PackageDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<PackageDetailDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var package = await _unitOfWork.Packages.GetWithServicesAsync(id, cancellationToken);
        if (package == null)
        {
            throw new EntityNotFoundException(nameof(Package), id);
        }

        return _mapper.Map<PackageDetailDto>(package);
    }

    public async Task<IReadOnlyList<ServiceDto>> GetRecentlyAddedServicesAsync(
        long packageId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        var package = await _unitOfWork.Packages.GetByIdAsync(packageId, cancellationToken);
        if (package == null)
        {
            throw new EntityNotFoundException(nameof(Package), packageId);
        }

        // Критерій 6.3: LINQ to Entities через проміжну сутність M:N
        var services = await _unitOfWork.Packages.GetRecentlyAddedServicesAsync(packageId, since, cancellationToken);
        return _mapper.Map<IReadOnlyList<ServiceDto>>(services);
    }

    public async Task<PackageDetailDto> CreateAsync(CreatePackageDto dto, CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Packages.ExistsByNameAsync(dto.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"Пакет послуг із назвою '{dto.Name}' уже існує.");
        }

        // Перевірка існування кожної послуги, що додається до пакета
        foreach (var item in dto.Services)
        {
            if (!await _unitOfWork.Services.ExistsByIdAsync(item.ServiceId, cancellationToken))
            {
                throw new EntityNotFoundException(nameof(Service), item.ServiceId);
            }
        }

        var package = new Package(
            dto.Name,
            dto.Price,
            dto.Description,
            dto.CreatedBy ?? "system");

        foreach (var item in dto.Services.OrderBy(s => s.SortOrder))
        {
            package.AddService(item.ServiceId, item.SortOrder);
        }

        await _unitOfWork.Packages.AddAsync(package, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(package.Id, cancellationToken);
    }

    public async Task<PackageDetailDto> UpdateAsync(long id, UpdatePackageDto dto, CancellationToken cancellationToken = default)
    {
        var package = await _unitOfWork.Packages.GetByIdAsync(id, cancellationToken);
        if (package == null)
        {
            throw new EntityNotFoundException(nameof(Package), id);
        }

        if (await _unitOfWork.Packages.ExistsByNameAsync(dto.Name, excludeId: id, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"Інший пакет із назвою '{dto.Name}' уже існує.");
        }

        package.UpdateDetails(dto.Name, dto.Price, dto.Description, dto.UpdatedBy);
        _unitOfWork.Packages.Update(package);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var package = await _unitOfWork.Packages.GetByIdAsync(id, cancellationToken);
        if (package == null)
        {
            throw new EntityNotFoundException(nameof(Package), id);
        }

        package.SoftDelete("system");
        _unitOfWork.Packages.Update(package);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PackageDetailDto> AddServiceAsync(
        long packageId,
        AddServiceToPackageDto dto,
        CancellationToken cancellationToken = default)
    {
        var package = await _unitOfWork.Packages.GetWithServicesAsync(packageId, cancellationToken);
        if (package == null)
        {
            throw new EntityNotFoundException(nameof(Package), packageId);
        }

        if (!await _unitOfWork.Services.ExistsByIdAsync(dto.ServiceId, cancellationToken))
        {
            throw new EntityNotFoundException(nameof(Service), dto.ServiceId);
        }

        package.AddService(dto.ServiceId, dto.SortOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(packageId, cancellationToken);
    }

    public async Task<PackageDetailDto> RemoveServiceAsync(
        long packageId,
        long serviceId,
        CancellationToken cancellationToken = default)
    {
        var package = await _unitOfWork.Packages.GetWithServicesAsync(packageId, cancellationToken);
        if (package == null)
        {
            throw new EntityNotFoundException(nameof(Package), packageId);
        }

        package.RemoveService(serviceId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(packageId, cancellationToken);
    }
}
