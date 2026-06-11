using Asp.Versioning;
using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Application.Statistics.Dtos;
using CovidAnalyticsPortal.Application.Statistics.Queries;
using CovidAnalyticsPortal.Application.Trends.Dtos;
using CovidAnalyticsPortal.Application.Trends.Queries;
using CovidAnalyticsPortal.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CovidAnalyticsPortal.API.Controllers.V1;

/// <summary>
/// Exposes analytical endpoints: state-level statistics (with country/state
/// filtering) and trend analysis over a metric and period.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AnalyticsController : ApiControllerBase
{
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsController"/> class.
    /// </summary>
    /// <param name="auditService">The audit service used to record analytics views.</param>
    public AnalyticsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Gets daily state-level statistics for a date range, optionally filtered
    /// to a single state.
    /// </summary>
    /// <param name="from">The inclusive start date of the range.</param>
    /// <param name="to">The inclusive end date of the range.</param>
    /// <param name="state">An optional state code or name to filter by.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The ordered list of state statistics.</returns>
    /// <response code="200">The statistics were retrieved successfully.</response>
    /// <response code="400">The supplied query parameters were invalid.</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(IReadOnlyList<StateStatisticDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<StateStatisticDto>>> GetStatistics(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string? state,
        CancellationToken cancellationToken)
    {
        var result = await Mediator
            .Send(new GetStateStatisticsQuery(from, to, state), cancellationToken)
            .ConfigureAwait(false);

        await _auditService
            .RecordAsync(
                AuditAction.ViewStatistics,
                "Viewed state statistics",
                nameof(AnalyticsController),
                $"from={from}; to={to}; state={state ?? "ALL"}",
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Gets the trend of a metric over a period, optionally scoped to a state,
    /// including the underlying time series for charting.
    /// </summary>
    /// <param name="metric">The metric to analyse.</param>
    /// <param name="from">The inclusive start date of the period.</param>
    /// <param name="to">The inclusive end date of the period.</param>
    /// <param name="state">An optional state code or name to scope the trend to.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The computed trend analysis.</returns>
    /// <response code="200">The trend analysis was computed successfully.</response>
    /// <response code="400">The supplied query parameters were invalid.</response>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(TrendDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TrendDto>> GetTrends(
        [FromQuery] MetricType metric,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string? state,
        CancellationToken cancellationToken)
    {
        var result = await Mediator
            .Send(new GetTrendAnalysisQuery(metric, from, to, state), cancellationToken)
            .ConfigureAwait(false);

        await _auditService
            .RecordAsync(
                AuditAction.ViewTrends,
                "Viewed trend analysis",
                nameof(AnalyticsController),
                $"metric={metric}; from={from}; to={to}; state={state ?? "ALL"}",
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }
}
