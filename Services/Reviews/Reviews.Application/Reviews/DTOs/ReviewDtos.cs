namespace Reviews.Application.Reviews.DTOs;

public record AuthorDto(long ClientId, string FullName, string? CarInfo);

public record ReviewCommentDto(string AuthorName, string Role, string Text, DateTime CreatedAt);

public record ReviewListDto(
    string Id,
    long ServiceId,
    string ServiceName,
    int Rating,
    string Title,
    string AuthorName,
    int CommentsCount,
    DateTime CreatedAt,
    int Version);

public record ReviewDetailDto(
    string Id,
    long ServiceId,
    string ServiceName,
    long? AppointmentId,
    string? DiscussionId,
    int Rating,
    string Title,
    string Text,
    bool VerifiedVisit,
    AuthorDto Author,
    IReadOnlyList<ReviewCommentDto> Comments,
    IReadOnlyList<string> PhotosBefore,
    IReadOnlyList<string> PhotosAfter,
    string? VideoUrl,
    IReadOnlyList<string> Pros,
    IReadOnlyList<string> Cons,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int Version);
