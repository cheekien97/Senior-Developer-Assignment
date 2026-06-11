using CovidAnalyticsPortal.Web.Models.ViewModels;
using CovidAnalyticsPortal.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CovidAnalyticsPortal.Web.Controllers;

/// <summary>
/// Serves the State Statistics page, allowing the user to query per-state
/// figures for a date range via the typed API client.
/// </summary>
public sealed class StatisticsController : Controller
{
    private readonly ICovidApiClient _apiClient;
    private readonly ILogger<StatisticsController> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="StatisticsController"/> class.
    /// </summary>
    /// <param name="apiClient">The typed API client.</param>
    /// <param name="logger">The logger.</param>
    public StatisticsController(ICovidApiClient apiClient, ILogger<StatisticsController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Renders the statistics page. When a date range is supplied the API is
    /// queried; otherwise an empty filter form is shown with a default window.
    /// </summary>
    /// <param name="from">Optional inclusive start date.</param>
    /// <param name="to">Optional inclusive end date.</param>
    /// <param name="state">Optional state code; empty returns all states.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The statistics view.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(
        DateOnly? from,
        DateOnly? to,
        string? state,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var model = new StateStatisticsViewModel
        {
            From = from ?? today.AddDays(-30),
            To = to ?? today,
            State = state,
            States = ReferenceData.BuildStateItems(state),
        };

        if (from is null && to is null)
        {
            return View(model);
        }

        try
        {
            model.Results = await _apiClient
                .GetStateStatisticsAsync(model.From, model.To, string.IsNullOrEmpty(state) ? null : state, cancellationToken)
                .ConfigureAwait(false);
            model.Searched = true;

            if (model.Results.Count == 0)
            {
                model.Message = "No statistics were found for the selected filters.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load state statistics from the API.");
            model.Message = "The analytics service is currently unavailable. Please try again later.";
        }

        return View(model);
    }
}
