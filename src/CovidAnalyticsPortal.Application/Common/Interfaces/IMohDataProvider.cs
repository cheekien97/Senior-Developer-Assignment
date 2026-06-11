namespace CovidAnalyticsPortal.Application.Common.Interfaces;

/// <summary>
/// A single normalised daily COVID-19 record sourced from the external Ministry
/// of Health (MoH) feed. This is the application's stable contract for ingested
/// data; the infrastructure layer is responsible for translating the upstream
/// payload into this shape.
/// </summary>
/// <param name="Date">The calendar day the record describes.</param>
/// <param name="StateNameOrCode">The state name or code, or <c>null</c> for a national record.</param>
/// <param name="NewCases">New confirmed cases for the day.</param>
/// <param name="CumulativeCases">Cumulative confirmed cases.</param>
/// <param name="ActiveCases">Currently active cases.</param>
/// <param name="Recovered">Recovered cases for the day.</param>
/// <param name="NewDeaths">New deaths for the day.</param>
/// <param name="CumulativeDeaths">Cumulative deaths.</param>
public sealed record MohDailyRecord(
    DateOnly Date,
    string? StateNameOrCode,
    long NewCases,
    long CumulativeCases,
    long ActiveCases,
    long Recovered,
    long NewDeaths,
    long CumulativeDeaths);

/// <summary>
/// Abstraction over the external Ministry of Health open-data feed
/// (<c>data.moh.gov.my</c>). Declared in the application layer so that the
/// domain and use cases depend only on this contract, while the concrete
/// HTTP integration lives in the infrastructure layer (Dependency Inversion).
/// </summary>
public interface IMohDataProvider
{
    /// <summary>
    /// Fetches the latest national daily COVID-19 records from the MoH feed.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A read-only list of normalised national records.</returns>
    Task<IReadOnlyList<MohDailyRecord>> GetNationalDailyAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the latest state-level daily COVID-19 records from the MoH feed.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A read-only list of normalised state-level records.</returns>
    Task<IReadOnlyList<MohDailyRecord>> GetStateDailyAsync(
        CancellationToken cancellationToken = default);
}
