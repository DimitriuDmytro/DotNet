namespace Bookings.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;
using Bookings.Domain.Exceptions;

/// <summary>
/// Контролер для читання та синхронізації локальної репліки каталогу послуг (Services).
/// </summary>
[ApiController]
[Route("api/catalog-services")]
public class CatalogServicesController : ControllerBase
{
    private readonly ICatalogQueryService _catalogQueryService;

    public CatalogServicesController(ICatalogQueryService catalogQueryService)
    {
        _catalogQueryService = catalogQueryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CatalogServiceDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var services = await _catalogQueryService.GetAllAsync(cancellationToken);
        return Ok(services);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(CatalogServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogServiceDto>> GetById(long id, CancellationToken cancellationToken = default)
    {
        var service = await _catalogQueryService.GetByIdAsync(id, cancellationToken);
        if (service == null)
        {
            throw new NotFoundException($"Послугу з ID #{id} не знайдено в локальному довіднику.");
        }

        return Ok(service);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert([FromBody] UpsertCatalogServiceDto dto, CancellationToken cancellationToken = default)
    {
        await _catalogQueryService.UpsertAsync(dto, cancellationToken);
        return NoContent();
    }
}
