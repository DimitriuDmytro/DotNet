namespace Catalog.Bll.Services;

using AutoMapper;
using Catalog.Bll.DTOs;
using Catalog.Bll.Interfaces;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

public class ServiceCategoryService : IServiceCategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ServiceCategoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IReadOnlyList<ServiceCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyList<ServiceCategoryDto>>(categories);
    }

    public async Task<ServiceCategoryDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetWithServicesAsync(id, cancellationToken);
        if (category == null)
        {
            throw new EntityNotFoundException(nameof(ServiceCategory), id);
        }

        return _mapper.Map<ServiceCategoryDto>(category);
    }

    public async Task<ServiceCategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Categories.ExistsByNameAsync(dto.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"Категорія з назвою '{dto.Name}' уже існує.");
        }

        var category = new ServiceCategory(dto.Name, dto.Description);
        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ServiceCategoryDto>(category);
    }

    public async Task<ServiceCategoryDto> UpdateAsync(long id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new EntityNotFoundException(nameof(ServiceCategory), id);
        }

        if (await _unitOfWork.Categories.ExistsByNameAsync(dto.Name, excludeId: id, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"Інша категорія з назвою '{dto.Name}' уже існує.");
        }

        category.Update(dto.Name, dto.Description);
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ServiceCategoryDto>(category);
    }
}
