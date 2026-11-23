namespace Reviews.Application.Reviews.Commands.UpdateServiceNameInReviews;

using MediatR;
using Microsoft.Extensions.Logging;
using global::Reviews.Application.Caching;
using global::Reviews.Domain.Interfaces;

/// <summary>
/// Обробник MediatR-команди для оновлення денормалізованої назви послуги (Критерій 10).
/// Оновлює колекції reviews, workReports та інвалідує кеш.
/// </summary>
public class UpdateServiceNameInReviewsCommandHandler : IRequestHandler<UpdateServiceNameInReviewsCommand>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IReviewsTwoLevelCache? _cache;
    private readonly ILogger<UpdateServiceNameInReviewsCommandHandler> _logger;

    public UpdateServiceNameInReviewsCommandHandler(
        IReviewRepository reviewRepository,
        ILogger<UpdateServiceNameInReviewsCommandHandler> logger,
        IReviewsTwoLevelCache? cache = null)
    {
        _reviewRepository = reviewRepository;
        _logger = logger;
        _cache = cache;
    }

    public async Task Handle(UpdateServiceNameInReviewsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Executing UpdateServiceNameInReviewsCommand for ServiceId {ServiceId} to '{NewName}' (OccurredAt: {OccurredAt})",
            request.ServiceId,
            request.NewName,
            request.OccurredAt);

        // 1. Оновлення колекції reviews із захистом від out-of-order
        var modifiedReviews = await _reviewRepository.UpdateServiceNameAsync(
            request.ServiceId,
            request.NewName,
            request.OccurredAt,
            cancellationToken);

        // 2. Оновлення колекції workReports
        var modifiedReports = await _reviewRepository.UpdateWorkReportsServiceNameAsync(
            request.ServiceId,
            request.NewName,
            request.OccurredAt,
            cancellationToken);

        _logger.LogInformation(
            "Updated {ModifiedReviews} reviews and {ModifiedWorkReports} workReports for ServiceId {ServiceId}",
            modifiedReviews,
            modifiedReports,
            request.ServiceId);

        // 3. Інвалідація дворівневого кешу L1/L2
        if (_cache != null)
        {
            await _cache.RemoveAsync($"reviews:service:{request.ServiceId}", "Service renamed via integration event", cancellationToken);
            await _cache.RemoveAsync("reviews:list:all", "Service renamed via integration event", cancellationToken);
        }
    }
}
