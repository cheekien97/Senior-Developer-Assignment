using CovidAnalyticsPortal.Web.Models.ViewModels;
using CovidAnalyticsPortal.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CovidAnalyticsPortal.Web.Controllers;

/// <summary>
/// Serves the Audit Trail page, allowing the recorded audit entries to be
/// reviewed and filtered by action and/or date range via the typed API client.
/// </summary>
public sealed class AuditController : Controller
{
    private const int DefaultMaxResults = 100;

    private readonly ICovidApiClient _apiClient;
    private readonly ILogger<AuditController> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="AuditController"/> class.
    /// </summary>
    /// <param name="apiClient">The typed API client.</param>
    /// <param name="logger">The logger.</param>
    public AuditController(ICovidApiClient apiClient, ILogger<AuditController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Renders the audit trail page, optionally filtered by action and date range.
    /// </summary>
    /// <param name="from">Optional inclusive start date.</param>
    /// <param name="to">Optional inclusive end date.</param>
    /// <param name="action">Optional action to filter by.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The audit trail view.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(
        DateOnly? from,
        DateOnly? to,
        [FromQuery] string? action,
        CancellationToken cancellationToken)
    {
        // Default the window to "first day of the current month .. today" so the
        // page opens on a sensible, populated range rather than an empty form.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveTo = to ?? today;
        var effectiveFrom = from ?? new DateOnly(today.Year, today.Month, 1);

        var model = new AuditTrailViewModel
        {
            From = effectiveFrom,
            To = effectiveTo,
            Action = action,
            Actions = ReferenceData.BuildAuditActionItems(action),
        };

        try
        {
            model.Results = await _apiClient
                .GetAuditTrailAsync(
                    effectiveFrom,
                    effectiveTo,
                    string.IsNullOrEmpty(action) ? null : action,
                    DefaultMaxResults,
                    cancellationToken)
                .ConfigureAwait(false);

            if (model.Results.Count == 0)
            {
                model.Message = "No audit entries were found for the selected filters.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load audit trail from the API.");
            model.Message = "The analytics service is currently unavailable. Please try again later.";
        }

        return View(model);
    }
}
