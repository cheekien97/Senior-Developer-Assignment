namespace CovidAnalyticsPortal.Web.Models.ApiContracts;

/// <summary>
/// Client-side mirror of the API's per-state statistic contract.
/// </summary>
public sealed class StateStatisticResponse
{
    /// <summary>Gets or sets the ISO-3166-2:MY state code (e.g. <c>MY-10</c>).</summary>
    public string StateCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable state name.</summary>
    public string StateName { get; set; } = string.Empty;

    /// <summary>Gets or sets the date the statistic refers to.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the new confirmed cases for the day.</summary>
    public long NewCases { get; set; }

    /// <summary>Gets or sets the cumulative confirmed cases.</summary>
    public long CumulativeCases { get; set; }

    /// <summary>Gets or sets the currently active cases.</summary>
    public long ActiveCases { get; set; }

    /// <summary>Gets or sets the recovered cases.</summary>
    public long Recovered { get; set; }

    /// <summary>Gets or sets the new deaths for the day.</summary>
    public long NewDeaths { get; set; }

    /// <summary>Gets or sets the cumulative deaths.</summary>
    public long CumulativeDeaths { get; set; }
}
