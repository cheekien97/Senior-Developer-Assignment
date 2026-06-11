using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CovidAnalyticsPortal.API.Controllers;

/// <summary>
/// Base class for the portal's API controllers. Centralises access to the
/// MediatR sender so derived controllers stay thin — they translate HTTP
/// requests into application queries/commands and return the result, with no
/// business logic of their own (Separation of Concerns).
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    /// <summary>
    /// Gets the MediatR sender, resolved lazily from the request services.
    /// </summary>
    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
