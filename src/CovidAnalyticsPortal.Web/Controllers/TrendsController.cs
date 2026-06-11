using CovidAnalyticsPortal.Web.Models.ViewModels;
using CovidAnalyticsPortal.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CovidAnalyticsPortal.Web.Controllers;

/// <summary>
/// Serves the Trend Analysis page, charting a chosen metric over time at the
/// national or per-state level via the typed API client.
/// </summary>
public sealed class TrendsController : Controller
{
    private readonly ICovidApiClient _apiClient;
    private readonly ILogger<TrendsController> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="TrendsController"/> class.
    /// </summary>
    /// <param name="apiClient">The typed API client.</param>
    /// <param name="logger">The logger.</param>
    public TrendsController(ICovidApiClient apiClient, ILogger<TrendsController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Renders the trend analysis page. When a metric and date range are
    /// supplied the API is queried; otherwise a default filter form is shown.
    /// </summary>
    /// <param name="metric">The metric to analyse.</param>
    /// <param name="from">Optional inclusive start date.</param>
    /// <param name="to">Optional inclusive end date.</param>
    /// <param name="state">Optional state code; empty produces a national trend.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The trend analysis view.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? metric,
        DateOnly? from,
        DateOnly? to,
        string? state,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var selectedMetric = string.IsNullOrWhiteSpace(metric) ? "Cases" : metric;

        // Default the filter to the latest available data month rather than
        // "today", since the upstream feed may lag well behind the current date.
        var (defaultFrom, defaultTo) = await ResolveDefaultRangeAsync(from, to, today, cancellationToken)
            .ConfigureAwait(false);

        var model = new TrendAnalysisViewModel
        {
            Metric = selectedMetric,
            From = from ?? defaultFrom,
            To = to ?? defaultTo,
            State = state,
            Metrics = ReferenceData.BuildMetricItems(selectedMetric),
            States = ReferenceData.BuildStateItems(state),
        };

        try
        {
            model.Trend = await _apiClient
                .GetTrendAsync(selectedMetric, model.From, model.To, string.IsNullOrEmpty(state) ? null : state, cancellationToken)
                .ConfigureAwait(false);
            model.Searched = true;

            if (model.Trend is null || model.Trend.Points.Count == 0)
            {
                model.Message = "No trend data was found for the selected filters.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load trend analysis from the API.");
            model.Message = "The analytics service is currently unavailable. Please try again later.";
        }

        return View(model);
    }

    private async Task<(DateOnly From, DateOnly To)> ResolveDefaultRangeAsync(
        DateOnly? from,
        DateOnly? to,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        // A complete range was supplied; no probe needed.
        if (from is not null && to is not null)
        {
            return (from.Value, to.Value);
        }

        try
        {
            var latest = await _apiClient
                .GetLatestDataDateAsync(cancellationToken)
                .ConfigureAwait(false);

            if (latest is { } d)
            {
                return (new DateOnly(d.Year, d.Month, 1), d);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to resolve the latest data date; using a recent window.");
        }

        return (today.AddDays(-30), today);
    }
}
