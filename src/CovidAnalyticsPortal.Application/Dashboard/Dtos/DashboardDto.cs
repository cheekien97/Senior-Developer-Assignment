using CovidAnalyticsPortal.Application.Statistics.Dtos;

namespace CovidAnalyticsPortal.Application.Dashboard.Dtos;

/// <summary>
/// Read-model for the dashboard summary. Aggregates national headline figures
/// for the reporting period along with a per-state breakdown for the most
/// recent day, ready for display in the MVC dashboard view.
/// </summary>
public sealed record DashboardDto
{
    /// <summary>Gets the most recent date represented in the summary.</summary>
    public required DateOnly AsOfDate { get; init; }

    /// <summary>Gets the inclusive start date of the reporting period.</summary>
    public required DateOnly PeriodStart { get; init; }

    /// <summary>Gets the inclusive end date of the reporting period.</summary>
    public required DateOnly PeriodEnd { get; init; }

    /// <summary>Gets the cumulative confirmed cases as of <see cref="AsOfDate"/>.</summary>
    public long TotalCases { get; init; }

    /// <summary>Gets the cumulative deaths as of <see cref="AsOfDate"/>.</summary>
    public long TotalDeaths { get; init; }

    /// <summary>Gets the number of currently active cases as of <see cref="AsOfDate"/>.</summary>
    public long ActiveCases { get; init; }

    /// <summary>Gets the total recovered cases over the reporting period.</summary>
    public long TotalRecovered { get; init; }

    /// <summary>Gets the new confirmed cases recorded on <see cref="AsOfDate"/>.</summary>
    public long NewCasesToday { get; init; }

    /// <summary>Gets the new deaths recorded on <see cref="AsOfDate"/>.</summary>
    public long NewDeathsToday { get; init; }

    /// <summary>
    /// Gets the per-state breakdown for the most recent day, ordered by new
    /// cases descending.
    /// </summary>
    public IReadOnlyList<StateStatisticDto> StateBreakdown { get; init; } =
        Array.Empty<StateStatisticDto>();
}
