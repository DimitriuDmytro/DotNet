namespace Reviews.WebApi.Grpc;

using System.Diagnostics;
using global::Grpc.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Application.Reviews.Queries.GetReviewsList;
using Reviews.Grpc;
using Reviews.WebApi.Metrics;

public sealed class ReviewsGrpcService : ReviewsService.ReviewsServiceBase
{
    private readonly ISender _sender;
    private readonly ReviewMetrics _metrics;
    private readonly ILogger<ReviewsGrpcService> _logger;

    public ReviewsGrpcService(
        ISender sender,
        ReviewMetrics metrics,
        ILogger<ReviewsGrpcService> logger)
    {
        _sender = sender;
        _metrics = metrics;
        _logger = logger;
    }

    public override async Task<ReviewReply> GetReviewById(GetReviewByIdRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Review ID must not be empty."));
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _sender.Send(new GetReviewByIdQuery(request.Id), context.CancellationToken);
            if (result is null)
            {
                sw.Stop();
                _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_review_by_id", "not_found");
                throw new RpcException(new Status(StatusCode.NotFound, $"Review with ID '{request.Id}' not found."));
            }

            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_review_by_id", "success");
            return ReviewsGrpcMappings.ToReply(result);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception)
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_review_by_id", "failure");
            throw;
        }
    }

    public override async Task<ReviewsReply> GetReviewsForService(GetReviewsForServiceRequest request, ServerCallContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var page = request.Page > 0 ? request.Page : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 20;

            var result = await _sender.Send(
                new GetReviewsListQuery(request.ServiceId > 0 ? request.ServiceId : null, page, pageSize),
                context.CancellationToken);

            var reply = new ReviewsReply
            {
                TotalCount = (int)result.TotalCount
            };

            if (result.Items is not null)
            {
                foreach (var item in result.Items)
                {
                    reply.Reviews.Add(ReviewsGrpcMappings.ToReply(item));
                }
            }

            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_reviews_for_service", "success");
            return reply;
        }
        catch (Exception)
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_reviews_for_service", "failure");
            throw;
        }
    }
}
