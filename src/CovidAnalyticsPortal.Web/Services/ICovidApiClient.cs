using CovidAnalyticsPortal.Web.Models.ApiContracts;

namespace CovidAnalyticsPortal.Web.Services;

/// <summary>
/// Abstraction over the backend COVID Analytics REST API. Implemented by a typed
/// <see cref="System.Net.Http.HttpClient"/> so controllers depend only on this
/// contract and never touch transport concerns.
/// </summary>
public interface ICovidApiClient
{
    /// <summary>
    /// Retrieves the national dashboard summary for the supplied period.
    /// </summary>
    /// <param name="from">Optional inclusive start date.</param>
    /// <param name="to">Optional inclusive end date.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The dashboard summary, or <c>null</c> if none is available.</returns>
    Task<DashboardResponse?> GetDashboardAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves per-state statistics for the supplied period and optional state.
    /// </summary>
    /// <param name="from">Inclusive start date.</param>
    /// <param name="to">Inclusive end date.</param>
    /// <param name="state">Optional state code; <c>null</c> returns all states.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching statistics, never <c>null</c>.</returns>
    Task<IReadOnlyList<StateStatisticResponse>> GetStateStatisticsAsync(
        DateOnly from,
        DateOnly to,
        string? state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a trend analysis for the supplied metric, period and optional state.
    /// </summary>
    /// <param name="metric">The metric name (matching the API's MetricType enum).</param>
    /// <param name="from">Inclusive start date.</param>
    /// <param name="to">Inclusive end date.</param>
    /// <param name="state">Optional state code; <c>null</c> produces a national trend.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The analysed trend, or <c>null</c> if none is available.</returns>
    Task<TrendResponse?> GetTrendAsync(
        string metric,
        DateOnly from,
        DateOnly to,
        string? state,
        CancellationToken cancellationToken = default);
}
