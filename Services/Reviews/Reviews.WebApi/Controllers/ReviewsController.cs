namespace Reviews.WebApi.Controllers;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Application.Reviews.Commands.DeleteReview;
using Reviews.Application.Reviews.Commands.UpdateReview;
using Reviews.Application.Reviews.DTOs;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Application.Reviews.Queries.GetReviewsList;
using Reviews.Domain.Exceptions;

/// <summary>
/// REST API контролер для управління відгуками клієнтів детейлінг-студії (Критерій 12).
/// Працює виключно через MediatR з підтримкою структурованого логування та трасування OpenTelemetry.
/// </summary>
public class ReviewsController : ApiController
{
    private static readonly ActivitySource Source = new("Reviews.WebApi");
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(ILogger<ReviewsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Отримання пагінованого списку відгуків з можливістю фільтрації за послугою.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReviewListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReviewListDto>>> GetList(
        [FromQuery] long? serviceId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        using var activity = Source.StartActivity("ReviewsController.GetList");
        activity?.SetTag("service.id", serviceId);

        _logger.LogInformation("ReviewsController: Запит списку відгуків для ServiceId={ServiceId}, сторінка {Page}", serviceId, page);
        var result = await Mediator.Send(new GetReviewsListQuery(serviceId, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Отримання детальної інформації про відгук за його MongoDB ObjectId.
    /// Повертає заголовок ETag для оптимістичного блокування при оновленні.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReviewDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewDetailDto>> GetById(string id, CancellationToken cancellationToken)
    {
        using var activity = Source.StartActivity("ReviewsController.GetById");
        activity?.SetTag("review.id", id);

        _logger.LogInformation("ReviewsController: Запит відгуку #{ReviewId}", id);
        var result = await Mediator.Send(new GetReviewByIdQuery(id), cancellationToken);

        if (result == null)
        {
            throw new NotFoundException($"Відгук з ID '{id}' не знайдено.");
        }

        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(result);
    }

    /// <summary>
    /// Створення нового відгуку на послугу.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateReviewCommand command, CancellationToken cancellationToken)
    {
        using var activity = Source.StartActivity("ReviewsController.Create");
        activity?.SetTag("review.service_id", command.ServiceId);
        activity?.SetTag("review.rating", command.Rating);

        _logger.LogInformation(
            "ReviewsController: Створення відгуку для послуги #{ServiceId} з оцінкою {Rating}",
            command.ServiceId,
            command.Rating);

        var id = await Mediator.Send(command, cancellationToken);
        activity?.SetTag("review.id", id);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>
    /// Оновлення існуючого відгуку з підтримкою оптимістичного блокування (ExpectedVersion / If-Match).
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateReviewCommand command,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        using var activity = Source.StartActivity("ReviewsController.Update");
        activity?.SetTag("review.id", id);

        _logger.LogInformation("ReviewsController: Оновлення відгуку #{ReviewId}", id);

        var expectedVersion = command.ExpectedVersion;
        if (!string.IsNullOrWhiteSpace(ifMatch))
        {
            var cleanIfMatch = ifMatch.Trim('\"', ' ');
            if (int.TryParse(cleanIfMatch, out var parsedVersion))
            {
                expectedVersion = parsedVersion;
            }
        }

        var effectiveCommand = command with { Id = id, ExpectedVersion = expectedVersion };
        await Mediator.Send(effectiveCommand, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Видалення відгуку за його ID.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        using var activity = Source.StartActivity("ReviewsController.Delete");
        activity?.SetTag("review.id", id);

        _logger.LogInformation("ReviewsController: Видалення відгуку #{ReviewId}", id);
        await Mediator.Send(new DeleteReviewCommand(id), cancellationToken);
        return NoContent();
    }
}
