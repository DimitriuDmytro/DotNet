namespace Catalog.Api.Controllers;

using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Catalog.Bll.DTOs;
using Catalog.Bll.Interfaces;

/// <summary>
/// Асинхронний контролер для управління комплексними пакетами послуг та зв'язками M:N (Критерії 6, 8, 11).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PackagesController : ControllerBase
{
    private readonly IPackageService _packageService;

    public PackagesController(IPackageService packageService)
    {
        _packageService = packageService;
    }

    /// <summary>
    /// Отримання пагінованого списку комплексних пакетів послуг.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PackageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PackageDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _packageService.GetPagedAsync(page, pageSize, search, minPrice, maxPrice, cancellationToken);

        Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Append("X-Page", result.Page.ToString());
        Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
        Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Отримання детальної інформації про пакет разом зі списком включених послуг (M:N).
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(PackageDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PackageDetailDto>> GetById(long id, CancellationToken cancellationToken = default)
    {
        var package = await _packageService.GetByIdAsync(id, cancellationToken);
        return Ok(package);
    }

    /// <summary>
    /// Критерій 6.3: отримання послуг пакета, доданих після зазначеної дати (LINQ через M:N проміжну сутність по полю AddedAt).
    /// </summary>
    [HttpGet("{id:long}/recently-added-services")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetRecentlyAddedServices(
        long id,
        [FromQuery] DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        var targetDate = since ?? DateTime.UtcNow.AddMonths(-6);
        var services = await _packageService.GetRecentlyAddedServicesAsync(id, targetDate, cancellationToken);
        return Ok(services);
    }

    /// <summary>
    /// Створення нового комплексного пакета послуг із валідацією через FluentValidation.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PackageDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PackageDetailDto>> Create(
        [FromBody] CreatePackageDto dto,
        [FromServices] IValidator<CreatePackageDto> validator,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var created = await _packageService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Оновлення інформації про пакет.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(PackageDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PackageDetailDto>> Update(
        long id,
        [FromBody] UpdatePackageDto dto,
        [FromServices] IValidator<UpdatePackageDto> validator,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var updated = await _packageService.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// М'яке видалення пакета (Soft Delete).
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default)
    {
        await _packageService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Додавання послуги до складу пакета (створення зв'язку M:N).
    /// </summary>
    [HttpPost("{id:long}/services")]
    [ProducesResponseType(typeof(PackageDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PackageDetailDto>> AddService(
        long id,
        [FromBody] AddServiceToPackageDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await _packageService.AddServiceAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Видалення послуги зі складу пакета (видалення зв'язку M:N).
    /// </summary>
    [HttpDelete("{id:long}/services/{serviceId:long}")]
    [ProducesResponseType(typeof(PackageDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PackageDetailDto>> RemoveService(
        long id,
        long serviceId,
        CancellationToken cancellationToken = default)
    {
        var updated = await _packageService.RemoveServiceAsync(id, serviceId, cancellationToken);
        return Ok(updated);
    }
}
