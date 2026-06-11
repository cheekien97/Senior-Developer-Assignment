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
        var model = new TrendAnalysisViewModel
        {
            Metric = selectedMetric,
            From = from ?? today.AddDays(-30),
            To = to ?? today,
            State = state,
            Metrics = ReferenceData.BuildMetricItems(selectedMetric),
            States = ReferenceData.BuildStateItems(state),
        };

        if (from is null && to is null)
        {
            return View(model);
        }

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
}
