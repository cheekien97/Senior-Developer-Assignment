namespace CovidAnalyticsPortal.Application.Statistics.Dtos;

/// <summary>
/// Read-model describing the COVID-19 statistics for a single state on a single
/// day. Flat, serialization-friendly shape intended for transfer across the API
/// boundary and consumption by the MVC presentation layer.
/// </summary>
public sealed record StateStatisticDto
{
    /// <summary>Gets the ISO 3166-2:MY subdivision code (e.g. <c>SGR</c>).</summary>
    public required string StateCode { get; init; }

    /// <summary>Gets the human-readable state name (e.g. <c>Selangor</c>).</summary>
    public required string StateName { get; init; }

    /// <summary>Gets the calendar day the statistics describe.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Gets the number of new confirmed cases for the day.</summary>
    public long NewCases { get; init; }

    /// <summary>Gets the cumulative confirmed cases up to and including the day.</summary>
    public long CumulativeCases { get; init; }

    /// <summary>Gets the number of currently active cases.</summary>
    public long ActiveCases { get; init; }

    /// <summary>Gets the number of recovered cases for the day.</summary>
    public long Recovered { get; init; }

    /// <summary>Gets the number of new deaths for the day.</summary>
    public long NewDeaths { get; init; }

    /// <summary>Gets the cumulative deaths up to and including the day.</summary>
    public long CumulativeDeaths { get; init; }
}
