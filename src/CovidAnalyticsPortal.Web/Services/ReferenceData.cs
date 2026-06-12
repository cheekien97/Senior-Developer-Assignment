using Microsoft.AspNetCore.Mvc.Rendering;

namespace CovidAnalyticsPortal.Web.Services;

/// <summary>
/// Provides static reference data for the UI filter dropdowns. The values
/// mirror the codes accepted by the backend API (ISO 3166-2:MY state codes and
/// the MetricType enum names) so selections round-trip cleanly.
/// </summary>
public static class ReferenceData
{
    /// <summary>
    /// The Malaysian states and federal territories keyed by their subdivision
    /// code, ordered for display.
    /// </summary>
    public static readonly IReadOnlyList<(string Code, string Name)> States =
    [
        ("JHR", "Johor"),
        ("KDH", "Kedah"),
        ("KTN", "Kelantan"),
        ("MLK", "Melaka"),
        ("NSN", "Negeri Sembilan"),
        ("PHG", "Pahang"),
        ("PRK", "Perak"),
        ("PLS", "Perlis"),
        ("PNG", "Pulau Pinang"),
        ("SBH", "Sabah"),
        ("SWK", "Sarawak"),
        ("SGR", "Selangor"),
        ("TRG", "Terengganu"),
        ("KUL", "W.P. Kuala Lumpur"),
        ("LBN", "W.P. Labuan"),
        ("PJY", "W.P. Putrajaya"),
    ];

    /// <summary>
    /// The analysable metrics, matching the API's MetricType enum names.
    /// </summary>
    public static readonly IReadOnlyList<(string Value, string Label)> Metrics =
    [
        ("Cases", "Confirmed Cases"),
        ("ActiveCases", "Active Cases"),
        ("Recovered", "Recovered"),
        ("Deaths", "Deaths"),
    ];

    /// <summary>
    /// The auditable actions, matching the API's AuditAction enum names.
    /// </summary>
    public static readonly IReadOnlyList<(string Value, string Label)> AuditActions =
    [
        ("ViewDashboard", "View Dashboard"),
        ("ViewStatistics", "View Statistics"),
        ("ViewTrends", "View Trends"),
        ("ViewHistory", "View History"),
        ("ApplyFilter", "Apply Filter"),
        ("ExportData", "Export Data"),
        ("IngestData", "Ingest Data"),
        ("SystemError", "System Error"),
    ];

    /// <summary>
    /// Builds the state dropdown items, with a leading "All states" option.
    /// </summary>
    /// <param name="selected">The currently selected state code, if any.</param>
    /// <returns>The select-list items.</returns>
    public static IReadOnlyList<SelectListItem> BuildStateItems(string? selected)
    {
        var items = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "All states (national)", Selected = string.IsNullOrEmpty(selected) },
        };

        items.AddRange(States.Select(s => new SelectListItem
        {
            Value = s.Code,
            Text = s.Name,
            Selected = string.Equals(s.Code, selected, StringComparison.OrdinalIgnoreCase),
        }));

        return items;
    }

    /// <summary>
    /// Builds the metric dropdown items.
    /// </summary>
    /// <param name="selected">The currently selected metric value.</param>
    /// <returns>The select-list items.</returns>
    public static IReadOnlyList<SelectListItem> BuildMetricItems(string? selected) =>
        Metrics.Select(m => new SelectListItem
        {
            Value = m.Value,
            Text = m.Label,
            Selected = string.Equals(m.Value, selected, StringComparison.OrdinalIgnoreCase),
        }).ToList();

    /// <summary>
    /// Builds the audit-action dropdown items, with a leading "All actions" option.
    /// </summary>
    /// <param name="selected">The currently selected action value, if any.</param>
    /// <returns>The select-list items.</returns>
    public static IReadOnlyList<SelectListItem> BuildAuditActionItems(string? selected)
    {
        var items = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "All actions", Selected = string.IsNullOrEmpty(selected) },
        };

        items.AddRange(AuditActions.Select(a => new SelectListItem
        {
            Value = a.Value,
            Text = a.Label,
            Selected = string.Equals(a.Value, selected, StringComparison.OrdinalIgnoreCase),
        }));

        return items;
    }
}
