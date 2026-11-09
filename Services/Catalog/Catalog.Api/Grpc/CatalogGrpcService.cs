namespace Catalog.Api.Grpc;

using Catalog.Bll.Interfaces;
using Catalog.Domain.Exceptions;
using Catalog.Grpc;
using global::Grpc.Core;
using Microsoft.Extensions.Logging;

public sealed class CatalogGrpcService : CatalogService.CatalogServiceBase
{
    private readonly IServiceService _serviceService;
    private readonly ILogger<CatalogGrpcService> _logger;

    public CatalogGrpcService(IServiceService serviceService, ILogger<CatalogGrpcService> logger)
    {
        _serviceService = serviceService;
        _logger = logger;
    }

    public override async Task<ServiceReply> GetService(GetServiceRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid service ID: {request.Id}"));
        }

        try
        {
            var service = await _serviceService.GetByIdAsync(request.Id, context.CancellationToken);
            if (service is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Service with ID {request.Id} not found"));
            }

            return CatalogGrpcMappings.ToReply(service);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "Service {ServiceId} not found in catalog", request.Id);
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<ServicesReply> GetServicesByIds(GetServicesByIdsRequest request, ServerCallContext context)
    {
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

        return reply;
    }
}
