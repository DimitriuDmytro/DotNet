namespace Reviews.WebApi.Grpc;

using Google.Protobuf.WellKnownTypes;
using Reviews.Application.Reviews.DTOs;
using Reviews.Grpc;

public static class ReviewsGrpcMappings
{
    public static Timestamp ToTimestamp(DateTime dateTime)
    {
        var utc = dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        return Timestamp.FromDateTime(utc);
    }

    public static ReviewReply ToReply(ReviewDetailDto dto)
    {
        return new ReviewReply
        {
            Id = dto.Id,
            ServiceId = dto.ServiceId,
            ServiceName = dto.ServiceName ?? string.Empty,
            Rating = dto.Rating,
            Title = dto.Title ?? string.Empty,
            Text = dto.Text ?? string.Empty,
            AuthorName = dto.Author?.FullName ?? string.Empty,
            CreatedAt = ToTimestamp(dto.CreatedAt)
        };
    }

    public static ReviewReply ToReply(ReviewListDto dto)
    {
        return new ReviewReply
        {
            Id = dto.Id,
            ServiceId = dto.ServiceId,
            ServiceName = dto.ServiceName ?? string.Empty,
            Rating = dto.Rating,
            Title = dto.Title ?? string.Empty,
            Text = string.Empty,
            AuthorName = dto.AuthorName ?? string.Empty,
            CreatedAt = ToTimestamp(dto.CreatedAt)
        };
    }
}
