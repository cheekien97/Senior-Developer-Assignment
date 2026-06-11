using CovidAnalyticsPortal.Web.Models.ViewModels;
using CovidAnalyticsPortal.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CovidAnalyticsPortal.Web.Controllers;

/// <summary>
/// Serves the national dashboard page, consuming the backend API through the
/// typed <see cref="ICovidApiClient"/>.
/// </summary>
public sealed class DashboardController : Controller
{
    private readonly ICovidApiClient _apiClient;
    private readonly ILogger<DashboardController> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="DashboardController"/> class.
    /// </summary>
    /// <param name="apiClient">The typed API client.</param>
    /// <param name="logger">The logger.</param>
    public DashboardController(ICovidApiClient apiClient, ILogger<DashboardController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Renders the dashboard for an optional date range (defaults to the API's
    /// own default window when no dates are supplied).
    /// </summary>
    /// <param name="from">Optional inclusive start date.</param>
    /// <param name="to">Optional inclusive end date.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The dashboard view.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var model = new DashboardViewModel { From = from, To = to };

        try
        {
            model.Summary = await _apiClient
                .GetDashboardAsync(from, to, cancellationToken)
                .ConfigureAwait(false);

            if (model.Summary is null)
            {
                model.Message = "No dashboard data is available for the selected period.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load dashboard data from the API.");
            model.Message = "The analytics service is currently unavailable. Please try again later.";
        }

        return View(model);
    }
}
