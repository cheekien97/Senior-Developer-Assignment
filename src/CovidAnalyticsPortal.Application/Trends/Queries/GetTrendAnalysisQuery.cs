using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Application.Trends.Dtos;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.ValueObjects;
using MediatR;

namespace CovidAnalyticsPortal.Application.Trends.Queries;

/// <summary>
/// CQRS query that requests trend analysis for a metric over an inclusive date
/// range, optionally scoped to a single state. Returns the computed trend and
/// its underlying time series for charting.
/// </summary>
/// <param name="Metric">The metric to analyse.</param>
/// <param name="From">The inclusive start date of the period.</param>
/// <param name="To">The inclusive end date of the period.</param>
/// <param name="State">The state code or name to scope to, or <c>null</c> for a national trend.</param>
public sealed record GetTrendAnalysisQuery(
    MetricType Metric,
    DateOnly From,
    DateOnly To,
    string? State = null) : IRequest<TrendDto>;

/// <summary>
/// Handles <see cref="GetTrendAnalysisQuery"/> by translating the primitive
/// inputs into domain value objects and delegating the computation to the
/// <see cref="IAnalyticsService"/>.
/// </summary>
public sealed class GetTrendAnalysisQueryHandler
    : IRequestHandler<GetTrendAnalysisQuery, TrendDto>
{
    private readonly IAnalyticsService _analyticsService;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GetTrendAnalysisQueryHandler"/> class.
    /// </summary>
    /// <param name="analyticsService">The analytics service performing the computation.</param>
    public GetTrendAnalysisQueryHandler(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <inheritdoc />
    public async Task<TrendDto> Handle(
        GetTrendAnalysisQuery request,
        CancellationToken cancellationToken)
    {
        var period = DateRange.Create(request.From, request.To);

        StateCode? state = null;
        if (!string.IsNullOrWhiteSpace(request.State))
        {
            // Validation has already confirmed the value is parseable.
            StateCode.TryParse(request.State, out state);
        }

        return await _analyticsService
            .AnalyzeTrendAsync(request.Metric, period, state, cancellationToken)
            .ConfigureAwait(false);
    }
}
