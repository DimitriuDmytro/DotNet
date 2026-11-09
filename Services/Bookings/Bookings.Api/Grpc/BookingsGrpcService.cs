namespace Bookings.Api.Grpc;

using Bookings.Bll.Interfaces;
using Bookings.Domain.Exceptions;
using Bookings.Grpc;
using global::Grpc.Core;
using Microsoft.Extensions.Logging;

public sealed class BookingsGrpcService : BookingsService.BookingsServiceBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IClientService _clientService;
    private readonly ILogger<BookingsGrpcService> _logger;

    public BookingsGrpcService(
        IAppointmentService appointmentService,
        IClientService clientService,
        ILogger<BookingsGrpcService> logger)
    {
        _appointmentService = appointmentService;
        _clientService = clientService;
        _logger = logger;
    }

    public override async Task<AppointmentReply> GetAppointment(GetAppointmentRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid appointment ID: {request.Id}"));
        }

        try
        {
            var appointment = await _appointmentService.GetByIdAsync(request.Id, context.CancellationToken);
            if (appointment is null)
            {
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

            return BookingsGrpcMappings.ToReply(appointment, clientFullName);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Appointment {AppointmentId} not found", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }
}
