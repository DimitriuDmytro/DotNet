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
using Reviews.WebApi.Metrics;

/// <summary>
/// REST API контролер для управління відгуками клієнтів детейлінг-студії (Критерій 12).
/// Працює через MediatR з підтримкою структурованого логування, бізнес-метрик та трасування OpenTelemetry.
/// </summary>
public class ReviewsController : ApiController
{
    private static readonly ActivitySource Source = new("Reviews.WebApi");
    private readonly ReviewMetrics _metrics;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(ReviewMetrics metrics, ILogger<ReviewsController> logger)
    {
        _metrics = metrics;
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
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("ReviewsController.GetList");
        activity?.SetTag("service.id", serviceId);

        _logger.LogInformation("ReviewsController: Запит списку відгуків для ServiceId={ServiceId}, сторінка {Page}", serviceId, page);
        try
        {
            var result = await Mediator.Send(new GetReviewsListQuery(serviceId, page, pageSize), cancellationToken);
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_list", "success");
            _metrics.SetTotalReviewsCount((int)result.TotalCount);
            return Ok(result);
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_list", "failure");
            throw;
        }
    }

    /// <summary>
    /// Отримання детальної інформації про відгук за його MongoDB ObjectId.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReviewDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewDetailDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("ReviewsController.GetById");
        activity?.SetTag("review.id", id);

        _logger.LogInformation("ReviewsController: Запит відгуку #{ReviewId}", id);
        try
        {
            var result = await Mediator.Send(new GetReviewByIdQuery(id), cancellationToken);

            if (result == null)
            {
                sw.Stop();
                _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_id", "not_found");
                throw new NotFoundException($"Відгук з ID '{id}' не знайдено.");
            }

            Response.Headers.ETag = $"\"{result.Version}\"";
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_id", "success");
            return Ok(result);
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "get_by_id", "failure");
            throw;
        }
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
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("ReviewsController.Create");
        activity?.SetTag("review.service_id", command.ServiceId);
        activity?.SetTag("review.rating", command.Rating);

        _logger.LogInformation(
            "ReviewsController: Створення відгуку для послуги #{ServiceId} з оцінкою {Rating}",
            command.ServiceId,
            command.Rating);

        try
        {
            var id = await Mediator.Send(command, cancellationToken);
            activity?.SetTag("review.id", id);

            _metrics.RecordReviewCreated();
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "create", "success");

            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "create", "failure");
            throw;
        }
    }

    /// <summary>
    /// Оновлення існуючого відгуку.
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
        var sw = Stopwatch.StartNew();
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
        try
        {
            await Mediator.Send(effectiveCommand, cancellationToken);
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "update", "success");
            return NoContent();
        }
        catch
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "update", "failure");
            throw;
        }
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
        var sw = Stopwatch.StartNew();
        using var activity = Source.StartActivity("ReviewsController.Delete");
        activity?.SetTag("review.id", id);

        _logger.LogInformation("ReviewsController: Видалення відгуку #{ReviewId}", id);
        try
        {
            await Mediator.Send(new DeleteReviewCommand(id), cancellationToken);
            _metrics.RecordReviewDeleted();
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
}
