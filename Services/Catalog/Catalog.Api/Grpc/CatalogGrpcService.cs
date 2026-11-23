namespace Catalog.Api.Grpc;

using System.Diagnostics;
using Catalog.Api.Metrics;
using Catalog.Bll.Interfaces;
using Catalog.Domain.Exceptions;
using Catalog.Grpc;
using global::Grpc.Core;
using Microsoft.Extensions.Logging;

public sealed class CatalogGrpcService : CatalogService.CatalogServiceBase
{
    private readonly IServiceService _serviceService;
    private readonly CatalogMetrics _metrics;
    private readonly ILogger<CatalogGrpcService> _logger;

    public CatalogGrpcService(
        IServiceService serviceService,
        CatalogMetrics metrics,
        ILogger<CatalogGrpcService> logger)
    {
        _serviceService = serviceService;
        _metrics = metrics;
        _logger = logger;
    }

    public override async Task<ServiceReply> GetService(GetServiceRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid service ID: {request.Id}"));
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var service = await _serviceService.GetByIdAsync(request.Id, context.CancellationToken);
            if (service is null)
            {
                sw.Stop();
                _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_service", "not_found");
                throw new RpcException(new Status(StatusCode.NotFound, $"Service with ID {request.Id} not found"));
            }

            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_service", "success");
            return CatalogGrpcMappings.ToReply(service);
        }
        catch (EntityNotFoundException ex)
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_service", "not_found");
            _logger.LogWarning(ex, "Service {ServiceId} not found in catalog", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception)
        {
            sw.Stop();
            _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_service", "failure");
            throw;
        }
    }

    public override async Task<ServicesReply> GetServicesByIds(GetServicesByIdsRequest request, ServerCallContext context)
    {
        var sw = Stopwatch.StartNew();
        var reply = new ServicesReply();

        foreach (var id in request.Ids.Distinct())
        {
            if (id <= 0) continue;
            try
            {
                var service = await _serviceService.GetByIdAsync(id, context.CancellationToken);
                if (service is not null)
                {
                    reply.Services.Add(CatalogGrpcMappings.ToReply(service));
                }
            }
            catch (EntityNotFoundException)
            {
                // У пакетному запиті відсутні послуги пропускаються
            }
        }

        sw.Stop();
        _metrics.RecordOperationDuration(sw.Elapsed.TotalMilliseconds, "grpc_get_services_batch", "success");
        return reply;
    }
}
