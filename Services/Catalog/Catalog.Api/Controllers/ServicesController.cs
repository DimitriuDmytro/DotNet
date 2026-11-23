namespace Catalog.Api.Controllers;

using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Platform.Security.Permissions;
using Catalog.Api.Metrics;
using Catalog.Bll.DTOs;
using Catalog.Bll.Interfaces;

/// <summary>
/// Асинхронний контролер для управління послугами детейлінг-студії (Критерії 8, 10, 11).
/// Тонкий шар представлення з підтримкою структурованого логування, метрик та трасування OpenTelemetry.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/products")]
[Route("products")]
[Authorize]
public class ServicesController : ControllerBase
{
    private static readonly ActivitySource Source = new("Catalog.Api");
    private readonly IServiceService _serviceService;
    private readonly CatalogMetrics _metrics;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IServiceService serviceService,
        CatalogMetrics metrics,
        ILogger<ServicesController> logger)
    {
        _serviceService = serviceService;
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Отримання пагінованого списку послуг із фільтрацією та сортуванням на рівні SQL (Критерій 10).
    /// </summary>
    [HttpGet]
    [RequirePermission("products:read")]
    [ProducesResponseType(typeof(PagedResult<ServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServiceDto>>> GetAll(
        [FromQuery] ServiceQuery query,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _serviceService.GetPagedAsync(query, cancellationToken);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_all", "success");
            _metrics.SetTotalServicesCount(result.TotalCount);

            return Ok(result);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_all", "failure");
            throw;
        }
    }

    /// <summary>
    /// Отримання послуги за ID разом із категорією, специфікацією, фотографіями та пакетами.
    /// </summary>
    [HttpGet("{id:long}")]
    [RequirePermission("products:read")]
    [ProducesResponseType(typeof(ServiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDetailDto>> GetById(long id, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("ServicesController.GetById");
        activity?.SetTag("service.id", id);

        _logger.LogInformation("ServicesController: Запит послуги #{ServiceId}", id);
        try
        {
            var service = await _serviceService.GetByIdAsync(id, cancellationToken);
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_id", "success");
            return Ok(service);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_id", "failure");
            throw;
        }
    }

    /// <summary>
    /// Отримання послуги за ID за допомогою явного завантаження (Explicit Loading).
    /// </summary>
    [HttpGet("{id:long}/explicit")]
    [ProducesResponseType(typeof(ServiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDetailDto>> GetByIdExplicit(long id, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("ServicesController.GetByIdExplicit");
        activity?.SetTag("service.id", id);

        try
        {
            var service = await _serviceService.GetByIdExplicitAsync(id, cancellationToken);
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_id_explicit", "success");
            return Ok(service);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_id_explicit", "failure");
            throw;
        }
    }

    /// <summary>
    /// Отримання послуг з гарантією від minWarrantyMonths місяців.
    /// </summary>
    [HttpGet("by-warranty")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> GetByMinWarranty(
        [FromQuery] int minWarrantyMonths = 6,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var services = await _serviceService.GetWithMinWarrantyAsync(minWarrantyMonths, cancellationToken);
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_warranty", "success");
            return Ok(services);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_warranty", "failure");
            throw;
        }
    }

    /// <summary>
    /// Створення нової послуги в каталозі.
    /// </summary>
    [HttpPost]
    [RequirePermission("products:create")]
    [ProducesResponseType(typeof(ServiceDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceDetailDto>> Create(
        [FromBody] CreateServiceDto dto,
        [FromServices] IValidator<CreateServiceDto> validator,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("ServicesController.Create");
        activity?.SetTag("service.name", dto.Name);
        activity?.SetTag("service.category_id", dto.CategoryId);

        _logger.LogInformation("ServicesController: Створення послуги '{ServiceName}' у категорії #{CategoryId}", dto.Name, dto.CategoryId);

        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        try
        {
            var created = await _serviceService.CreateAsync(dto, cancellationToken);
            activity?.SetTag("service.id", created.Id);

            _metrics.RecordServiceCreated();
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "create", "success");

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "create", "failure");
            throw;
        }
    }

    /// <summary>
    /// Оновлення інформації про послугу.
    /// </summary>
    [HttpPut("{id:long}")]
    [RequirePermission("products:update")]
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
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("ServicesController.Update");
        activity?.SetTag("service.id", id);

        _logger.LogInformation("ServicesController: Оновлення послуги #{ServiceId}", id);

        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        try
        {
            var updated = await _serviceService.UpdateAsync(id, dto, cancellationToken);
            _metrics.RecordServiceUpdated();
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "update", "success");
            return Ok(updated);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "update", "failure");
            throw;
        }
    }

    /// <summary>
    /// Зміна вартості послуги із доменною валідацією.
    /// </summary>
    [HttpPatch("{id:long}/price")]
    [RequirePermission("products:update")]
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
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("ServicesController.UpdatePrice");
        activity?.SetTag("service.id", id);
        activity?.SetTag("service.price", dto.NewPrice);

        _logger.LogInformation("ServicesController: Зміна ціни послуги #{ServiceId} на {NewPrice}", id, dto.NewPrice);

        var validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        try
        {
            var updated = await _serviceService.UpdatePriceAsync(id, dto, cancellationToken);
            _metrics.RecordPriceChanged();
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "update_price", "success");
            return Ok(updated);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "update_price", "failure");
            throw;
        }
    }

    /// <summary>
    /// М'яке видалення послуги (Soft Delete).
    /// </summary>
    [HttpDelete("{id:long}")]
    [RequirePermission("products:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("ServicesController.Delete");
        activity?.SetTag("service.id", id);

        _logger.LogInformation("ServicesController: Видалення послуги #{ServiceId}", id);
        try
        {
            await _serviceService.DeleteAsync(id, cancellationToken);
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "delete", "success");
            return NoContent();
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "delete", "failure");
            throw;
        }
    }

    [HttpPost("{id:long}/publish")]
    [RequirePermission("products:publish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(long id, CancellationToken cancellationToken = default)
    {
        var service = await _serviceService.GetByIdAsync(id, cancellationToken);
        if (service == null)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Service #{id} not found" });
        }

        _logger.LogInformation("ServicesController: Публікація послуги #{ServiceId}", id);
        return Ok(new { message = $"Service #{id} published successfully", serviceId = id });
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
        using var activity = Source.StartActivity("ServicesController.AddImage");
        activity?.SetTag("service.id", id);

        _logger.LogInformation("ServicesController: Додавання фото до послуги #{ServiceId}", id);
        var image = await _serviceService.AddImageAsync(id, dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, image);
    }

    /// <summary>
    /// Видалення фотографії з галереї послуги.
    /// </summary>
    [HttpDelete("{serviceId:long}/images/{imageId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveImage(long serviceId, long imageId, CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("ServicesController.RemoveImage");
        activity?.SetTag("service.id", serviceId);
        activity?.SetTag("image.id", imageId);

        _logger.LogInformation("ServicesController: Видалення фото #{ImageId} з послуги #{ServiceId}", imageId, serviceId);
        await _serviceService.RemoveImageAsync(serviceId, imageId, cancellationToken);
        return NoContent();
    }
}
