using CovidAnalyticsPortal.Application.Dashboard.Dtos;
using CovidAnalyticsPortal.Application.Statistics.Dtos;
using CovidAnalyticsPortal.Application.Trends.Dtos;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.ValueObjects;

namespace CovidAnalyticsPortal.Application.Common.Interfaces;

/// <summary>
/// Application service that assembles the dashboard summary. Encapsulates the
/// aggregation logic so it can be reused by query handlers and tested in
/// isolation. Defined as an interface to honour the Dependency Inversion and
/// Interface Segregation principles.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Builds the dashboard summary covering the supplied period.
    /// </summary>
    /// <param name="period">The reporting period for the dashboard.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The assembled <see cref="DashboardDto"/>.</returns>
    Task<DashboardDto> BuildDashboardAsync(DateRange period, CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service responsible for analytical computations such as state
/// statistics retrieval and trend analysis. Kept separate from the dashboard
/// service to respect the Single Responsibility Principle.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Retrieves the daily statistics for a state (or all states when none is
    /// specified) over the supplied period.
    /// </summary>
    /// <param name="state">The state to filter by, or <c>null</c> for all states.</param>
    /// <param name="period">The reporting period.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>An ordered, read-only list of <see cref="StateStatisticDto"/>.</returns>
    Task<IReadOnlyList<StateStatisticDto>> GetStateStatisticsAsync(
        StateCode? state,
        DateRange period,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the trend of a metric over a period, optionally scoped to a
    /// state, including the full time series used for charting.
    /// </summary>
    /// <param name="metric">The metric to analyse.</param>
    /// <param name="period">The period to analyse.</param>
    /// <param name="state">The state to scope to, or <c>null</c> for national.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The computed <see cref="TrendDto"/>.</returns>
    Task<TrendDto> AnalyzeTrendAsync(
        MetricType metric,
        DateRange period,
        StateCode? state,
        CancellationToken cancellationToken = default);
}
