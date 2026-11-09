namespace Bookings.Api.Grpc;

using Bookings.Bll.DTOs;
using Bookings.Grpc;
using Google.Protobuf.WellKnownTypes;

public static class BookingsGrpcMappings
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

    public static Timestamp ToTimestamp(DateTime dateTime)
    {
        var utc = dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        return Timestamp.FromDateTime(utc);
    }

    public static AppointmentReply ToReply(AppointmentDto dto, string? clientFullName = null)
    {
        var reply = new AppointmentReply
        {
            Id = dto.Id,
            ClientId = dto.ClientId,
            ClientFullName = clientFullName ?? $"Client #{dto.ClientId}",
            ScheduledAt = ToTimestamp(dto.ScheduledAt),
            Status = dto.Status ?? string.Empty,
            CarBrand = dto.Details?.CarBrand ?? string.Empty,
            CarModel = dto.Details?.CarModel ?? string.Empty,
            TotalAmount = ToDecimalValue(dto.TotalAmount)
        };

        if (dto.Items is not null)
        {
            foreach (var item in dto.Items)
            {
                reply.Items.Add(new AppointmentItemReply
                {
                    Id = item.AppointmentId,
                    ServiceId = item.ServiceId,
                    ServiceName = item.ServiceName ?? string.Empty,
                    UnitPrice = ToDecimalValue(item.UnitPrice),
                    Quantity = item.Quantity,
                    Discount = ToDecimalValue(item.Discount)
                });
            }
        }

        return reply;
    }
}
