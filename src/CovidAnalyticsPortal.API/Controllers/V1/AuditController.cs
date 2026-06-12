using Asp.Versioning;
using CovidAnalyticsPortal.Application.Audit.Dtos;
using CovidAnalyticsPortal.Application.Audit.Queries;
using CovidAnalyticsPortal.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CovidAnalyticsPortal.API.Controllers.V1;

/// <summary>
/// Exposes the portal's audit trail for review, allowing the recorded entries
/// to be queried by action and/or date range. The audit trail is append-only;
/// this controller offers read access only.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AuditController : ApiControllerBase
{
    /// <summary>
    /// Gets audit trail entries, optionally filtered by action and/or date
    /// range, ordered most-recent first.
    /// </summary>
    /// <param name="from">The inclusive start date, or <c>null</c> for no lower bound.</param>
    /// <param name="to">The inclusive end date, or <c>null</c> for no upper bound.</param>
    /// <param name="action">An optional action to filter by.</param>
    /// <param name="maxResults">The maximum number of entries to return (defaults to 100).</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The ordered list of audit trail entries.</returns>
    /// <response code="200">The audit trail entries were retrieved successfully.</response>
    /// <response code="400">The supplied query parameters were invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditTrailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AuditTrailDto>>> GetAuditTrail(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] AuditAction? action,
        [FromQuery] int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator
            .Send(new GetAuditTrailQuery(from, to, action, maxResults), cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }
}
