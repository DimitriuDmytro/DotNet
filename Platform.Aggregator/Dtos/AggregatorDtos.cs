namespace Platform.Aggregator.Dtos;

public record OrderSummaryDto(
    long Id,
    long ClientId,
    string ClientName,
    DateTime ScheduledAt,
    string Status,
    string CarInfo,
    decimal TotalAmount);

public record ProductDto(
    long Id,
    long CategoryId,
    string Name,
    decimal Price,
    int DurationMin);

public record ReviewDto(
    string Id,
    long ServiceId,
    string ServiceName,
    int Rating,
    string Title,
    string Text,
    string AuthorName,
    DateTime CreatedAt);

public record OrderLineDto(
    long ServiceId,
    string ServiceName,
    decimal UnitPrice,
    int Quantity,
    decimal Discount,
    ProductDto? Product,
    IReadOnlyList<ReviewDto> Reviews);

public record FullOrderDto(
    OrderSummaryDto Order,
    IReadOnlyList<OrderLineDto> Lines,
    IReadOnlyList<string> Warnings);

// Допоміжні DTO для відповідей мікросервісів
public record AppointmentApiResponse(
    long Id,
    long ClientId,
    string? ClientFullName,
    DateTime ScheduledAt,
    string Status,
    string? CarBrand,
    string? CarModel,
    decimal TotalAmount,
    List<AppointmentItemApiResponse>? Items);

public record AppointmentItemApiResponse(
    long Id,
    long ServiceId,
    string ServiceName,
    decimal UnitPrice,
    int Quantity,
    decimal Discount);

public record ReviewsPagedApiResponse(
    List<ReviewItemApiResponse>? Items,
    int TotalCount);

public record ReviewItemApiResponse(
    string Id,
    long ServiceId,
    string ServiceName,
    int Rating,
    string Title,
    string? AuthorName,
    DateTime CreatedAt);
