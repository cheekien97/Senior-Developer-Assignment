using Asp.Versioning;
using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Application.Dashboard.Dtos;
using CovidAnalyticsPortal.Application.Dashboard.Queries;
using CovidAnalyticsPortal.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CovidAnalyticsPortal.API.Controllers.V1;

/// <summary>
/// Exposes the COVID-19 dashboard summary, aggregating national headline
/// figures and a per-state breakdown for the most recent reporting day.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardController"/> class.
    /// </summary>
    /// <param name="auditService">The audit service used to record dashboard views.</param>
    public DashboardController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Gets the dashboard summary for an optional reporting period.
    /// </summary>
    /// <param name="from">The inclusive start date (defaults to a 30-day window).</param>
    /// <param name="to">The inclusive end date (defaults to today).</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The aggregated dashboard summary.</returns>
    /// <response code="200">The dashboard summary was retrieved successfully.</response>
    /// <response code="400">The supplied query parameters were invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardDto>> GetDashboard(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await Mediator
            .Send(new GetDashboardQuery(from, to), cancellationToken)
            .ConfigureAwait(false);

        await _auditService
            .RecordAsync(
                AuditAction.ViewDashboard,
                "Viewed dashboard summary",
                nameof(DashboardController),
                $"from={from}; to={to}",
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }
}
