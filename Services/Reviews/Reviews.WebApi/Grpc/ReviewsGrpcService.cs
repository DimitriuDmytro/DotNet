namespace Reviews.WebApi.Grpc;

using global::Grpc.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Application.Reviews.Queries.GetReviewsList;
using Reviews.Grpc;

public sealed class ReviewsGrpcService : ReviewsService.ReviewsServiceBase
{
    private readonly ISender _sender;
    private readonly ILogger<ReviewsGrpcService> _logger;

    public ReviewsGrpcService(ISender sender, ILogger<ReviewsGrpcService> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public override async Task<ReviewReply> GetReviewById(GetReviewByIdRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Review ID must not be empty."));
        }

        var result = await _sender.Send(new GetReviewByIdQuery(request.Id), context.CancellationToken);
        if (result is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Review with ID '{request.Id}' not found."));
        }

        return ReviewsGrpcMappings.ToReply(result);
    }

    public override async Task<ReviewsReply> GetReviewsForService(GetReviewsForServiceRequest request, ServerCallContext context)
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

        return reply;
    }
}
