namespace Reviews.WebApi.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Базовий API-контролер з доступом до MediatR ISender (Критерій 12).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiController : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
