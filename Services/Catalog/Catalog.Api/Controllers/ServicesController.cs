namespace Catalog.Api.Controllers;

using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Catalog.Bll.DTOs;
using Catalog.Bll.Interfaces;

/// <summary>
/// Асинхронний контролер для управління послугами детейлінг-студії (Критерії 8, 10, 11).
/// Тонкий шар представлення з підтримкою структурованого логування та трасування OpenTelemetry.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private static readonly ActivitySource Source = new("Catalog.Api");
    private readonly IServiceService _serviceService;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IServiceService serviceService,
        ILogger<ServicesController> logger)
    {
        _serviceService = serviceService;
        _logger = logger;
    }

    /// <summary>
    /// Отримання пагінованого списку послуг із фільтрацією та сортуванням на рівні SQL (Критерій 10).
    /// Метадані пагінації повертаються як у тілі відповіді PagedResult, так і в HTTP-заголовках X-*.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServiceDto>>> GetAll(
        [FromQuery] ServiceQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _serviceService.GetPagedAsync(query, cancellationToken);

        // Критерій 10: повернення метаданих пагінації в HTTP-заголовках
        Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Append("X-Page", result.Page.ToString());
        Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
        Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Отримання послуги за ID разом із категорією, специфікацією, фотографіями та пакетами.
    /// Критерій 6.1 (Eager Loading): виконує РІВНО 1 SQL-запит (Include/ThenInclude).
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ServiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDetailDto>> GetById(long id, CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("ServicesController.GetById");
        activity?.SetTag("service.id", id);

        _logger.LogInformation("ServicesController: Запит послуги #{ServiceId}", id);
        var service = await _serviceService.GetByIdAsync(id, cancellationToken);
        return Ok(service);
    }

    /// <summary>
    /// Отримання послуги за ID за допомогою явного завантаження (Explicit Loading).
    /// Критерій 6.2: виконує 2+ окремих SQL-запитів через Entry().Reference/Collection.LoadAsync().
    /// </summary>
    [HttpGet("{id:long}/explicit")]
    [ProducesResponseType(typeof(ServiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDetailDto>> GetByIdExplicit(long id, CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("ServicesController.GetByIdExplicit");
        activity?.SetTag("service.id", id);

        var service = await _serviceService.GetByIdExplicitAsync(id, cancellationToken);
        return Ok(service);
    }

    /// <summary>
    /// Отримання послуг з гарантією від minWarrantyMonths місяців (Критерій 14, друга специфікація).
    /// </summary>
    [HttpGet("by-warranty")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetByMinWarranty(
        [FromQuery] int minWarrantyMonths = 6,
        CancellationToken cancellationToken = default)
    {
        var services = await _serviceService.GetWithMinWarrantyAsync(minWarrantyMonths, cancellationToken);
        return Ok(services);
    }

    /// <summary>
    /// Створення нової послуги в каталозі (Критерій 8 та 11).
    /// Валідація через FluentValidation -> 400 ValidationProblemDetails, перевірка унікальності -> 409 Conflict.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ServiceDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceDetailDto>> Create(
        [FromBody] CreateServiceDto dto,
        [FromServices] IValidator<CreateServiceDto> validator,
        CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("ServicesController.Create");
        activity?.SetTag("service.name", dto.Name);
        activity?.SetTag("service.category_id", dto.CategoryId);

        _logger.LogInformation("ServicesController: Створення послуги '{ServiceName}' у категорії #{CategoryId}", dto.Name, dto.CategoryId);

        // Критерій 11: явний виклик FluentValidation валідатора
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var created = await _serviceService.CreateAsync(dto, cancellationToken);
        activity?.SetTag("service.id", created.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Оновлення інформації про послугу.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ServiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceDetailDto>> Update(
        long id,
        [FromBody] UpdateServiceDto dto,
        [FromServices] IValidator<UpdateServiceDto> validator,
        CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("ServicesController.Update");
        activity?.SetTag("service.id", id);

        _logger.LogInformation("ServicesController: Оновлення послуги #{ServiceId}", id);

        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var updated = await _serviceService.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Зміна вартості послуги із доменною валідацією.
    /// </summary>
    [HttpPatch("{id:long}/price")]
    [ProducesResponseType(typeof(ServiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceDetailDto>> UpdatePrice(
        long id,
        [FromBody] UpdatePriceDto dto,
        [FromServices] IValidator<UpdatePriceDto> validator,
        CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("ServicesController.UpdatePrice");
        activity?.SetTag("service.id", id);
        activity?.SetTag("service.price", dto.NewPrice);

        _logger.LogInformation("ServicesController: Зміна ціни послуги #{ServiceId} на {NewPrice}", id, dto.NewPrice);

        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var updated = await _serviceService.UpdatePriceAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// М'яке видалення послуги (Soft Delete).
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("ServicesController.Delete");
        activity?.SetTag("service.id", id);

        _logger.LogInformation("ServicesController: Видалення послуги #{ServiceId}", id);
        await _serviceService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Додавання фотографії до галереї послуги.
    /// </summary>
    [HttpPost("{id:long}/images")]
    [ProducesResponseType(typeof(ServiceImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceImageDto>> AddImage(
        long id,
        [FromBody] AddServiceImageDto dto,
        CancellationToken cancellationToken = default)
    {
        var image = await _serviceService.AddImageAsync(id, dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, image);
    }

    /// <summary>
    /// Видалення фотографії з галереї послуги.
    /// </summary>
    [HttpDelete("{id:long}/images/{imageId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveImage(long id, long imageId, CancellationToken cancellationToken = default)
    {
        await _serviceService.RemoveImageAsync(id, imageId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Додавання або оновлення технічної специфікації послуги (1:1).
    /// </summary>
    [HttpPut("{id:long}/spec")]
    [ProducesResponseType(typeof(ServiceSpecDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceSpecDto>> UpsertSpec(
        long id,
        [FromBody] UpsertServiceSpecDto dto,
        CancellationToken cancellationToken = default)
    {
        var spec = await _serviceService.UpsertSpecAsync(id, dto, cancellationToken);
        return Ok(spec);
    }
}
