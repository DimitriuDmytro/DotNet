namespace Catalog.Api.Grpc;

using Catalog.Bll.DTOs;
using Catalog.Grpc;

public static class CatalogGrpcMappings
{
    private const decimal NanoFactor = 1_000_000_000m;

    public static DecimalValue ToDecimalValue(decimal value)
    {
        var units = decimal.ToInt64(value);
        var nanos = decimal.ToInt32((value - units) * NanoFactor);
        return new DecimalValue
        {
            Units = units,
            Nanos = nanos
        };
    }

    public static decimal ToDecimal(DecimalValue value)
    {
        return value.Units + (value.Nanos / NanoFactor);
    }

    public static ServiceReply ToReply(ServiceDetailDto dto)
    {
        return new ServiceReply
        {
            Id = dto.Id,
            CategoryId = dto.CategoryId,
            Name = dto.Name ?? string.Empty,
            Price = ToDecimalValue(dto.Price),
            DurationMinutes = dto.DurationMin,
            Description = dto.Spec?.Description ?? string.Empty,
            IsActive = true
        };
    }

    public static ServiceReply ToReply(ServiceDto dto)
    {
        return new ServiceReply
        {
            Id = dto.Id,
            CategoryId = dto.CategoryId,
            Name = dto.Name ?? string.Empty,
            Price = ToDecimalValue(dto.Price),
            DurationMinutes = dto.DurationMin,
            Description = string.Empty,
            IsActive = true
        };
    }
}
