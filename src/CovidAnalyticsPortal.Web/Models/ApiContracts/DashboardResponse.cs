namespace CovidAnalyticsPortal.Web.Models.ApiContracts;

/// <summary>
/// Client-side mirror of the API's dashboard summary contract. Declared in the
/// Web project (rather than referencing the Application layer) so the MVC client
/// depends only on the HTTP contract, preserving the Clean Architecture
/// boundary.
/// </summary>
public sealed class DashboardResponse
{
    /// <summary>Gets or sets the most recent date represented in the summary.</summary>
    public DateOnly AsOfDate { get; set; }

    /// <summary>Gets or sets the inclusive start date of the reporting period.</summary>
    public DateOnly PeriodStart { get; set; }

    /// <summary>Gets or sets the inclusive end date of the reporting period.</summary>
    public DateOnly PeriodEnd { get; set; }

    /// <summary>Gets or sets the cumulative confirmed cases as of the summary date.</summary>
    public long TotalCases { get; set; }

    /// <summary>Gets or sets the cumulative deaths as of the summary date.</summary>
    public long TotalDeaths { get; set; }

    /// <summary>Gets or sets the number of currently active cases.</summary>
    public long ActiveCases { get; set; }

    /// <summary>Gets or sets the total recovered cases over the reporting period.</summary>
    public long TotalRecovered { get; set; }

    /// <summary>Gets or sets the new confirmed cases recorded on the summary date.</summary>
    public long NewCasesToday { get; set; }

    /// <summary>Gets or sets the new deaths recorded on the summary date.</summary>
    public long NewDeathsToday { get; set; }

    /// <summary>Gets or sets the per-state breakdown for the most recent day.</summary>
    public List<StateStatisticResponse> StateBreakdown { get; set; } = [];
}
