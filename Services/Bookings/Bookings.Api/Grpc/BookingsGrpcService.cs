namespace Bookings.Api.Grpc;

using System.Diagnostics;
using Bookings.Api.Metrics;
using Bookings.Bll.Interfaces;
using Bookings.Domain.Exceptions;
using Bookings.Grpc;
using global::Grpc.Core;
using Microsoft.Extensions.Logging;

public sealed class BookingsGrpcService : BookingsService.BookingsServiceBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IClientService _clientService;
    private readonly BookingMetrics _metrics;
    private readonly ILogger<BookingsGrpcService> _logger;

    public BookingsGrpcService(
        IAppointmentService appointmentService,
        IClientService clientService,
        BookingMetrics metrics,
        ILogger<BookingsGrpcService> logger)
    {
        _appointmentService = appointmentService;
        _clientService = clientService;
        _metrics = metrics;
        _logger = logger;
    }

    public override async Task<AppointmentReply> GetAppointment(GetAppointmentRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid appointment ID: {request.Id}"));
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var appointment = await _appointmentService.GetByIdAsync(request.Id, context.CancellationToken);
            if (appointment is null)
            {
                sw.Stop();
                _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_appointment", "not_found");
                throw new RpcException(new Status(StatusCode.NotFound, $"Appointment with ID {request.Id} not found"));
            }

            string clientFullName = $"Client #{appointment.ClientId}";
            try
            {
                var client = await _clientService.GetByIdAsync(appointment.ClientId, context.CancellationToken);
                if (client is not null && !string.IsNullOrWhiteSpace(client.FullName))
                {
                    clientFullName = client.FullName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve client {ClientId} name for appointment {AppointmentId}", appointment.ClientId, request.Id);
            }

            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_appointment", "success");
            return BookingsGrpcMappings.ToReply(appointment, clientFullName);
        }
        catch (NotFoundException ex)
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_appointment", "not_found");
            _logger.LogWarning(ex, "Appointment {AppointmentId} not found", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception)
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_appointment", "failure");
            throw;
        }
    }
}
