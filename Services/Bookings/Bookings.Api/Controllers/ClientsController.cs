namespace Bookings.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Bookings.Bll.DTOs;
using Bookings.Bll.Interfaces;
using Bookings.Domain.Exceptions;

/// <summary>
/// Контролер для керування клієнтами детейлінг-студії.
/// Не містить бізнес-логіки чи SQL-запитів — делегує виклики до IClientService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClientDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClientDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _clientService.GetAllAsync(pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDto>> GetById(long id, CancellationToken cancellationToken = default)
    {
        var client = await _clientService.GetByIdAsync(id, cancellationToken);
        if (client == null)
        {
            throw new NotFoundException($"Клієнта з ID #{id} не знайдено.");
        }

        return Ok(client);
    }

    [HttpGet("by-email")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDto>> GetByEmail([FromQuery] string email, CancellationToken cancellationToken = default)
    {
        var client = await _clientService.GetByEmailAsync(email, cancellationToken);
        if (client == null)
        {
            throw new NotFoundException($"Клієнта з email '{email}' не знайдено.");
        }

        return Ok(client);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientDto>> Create([FromBody] CreateClientDto dto, CancellationToken cancellationToken = default)
    {
        var created = await _clientService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientDto>> Update(long id, [FromBody] UpdateClientDto dto, CancellationToken cancellationToken = default)
    {
        var updated = await _clientService.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, [FromQuery] string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        await _clientService.DeleteAsync(id, deletedBy, cancellationToken);
        return NoContent();
    }
}
